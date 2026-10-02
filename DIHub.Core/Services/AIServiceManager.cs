using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;
using Microsoft.Extensions.Logging;

namespace DIHub.Core.Services
{
    public sealed class AIServiceManager : IAIServiceManager
    {
        private const int CurrentConfigVersion = 7;

        private readonly IConfigurationStorage _storage;
        private readonly ILogger<AIServiceManager> _logger;
        private readonly List<AIService> _services = new();

        public IReadOnlyList<AIService> Services => _services;

        public event EventHandler? ServicesChanged;

        public AIServiceManager(IConfigurationStorage storage, ILogger<AIServiceManager> logger)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task LoadAsync(CancellationToken ct = default)
        {
            try
            {
                var config = await _storage.LoadAsync(ct);
                _services.Clear();

                if (config.Services.Count == 0)
                {
                    _services.AddRange(CreateDefaults());
                    await SaveAsync(ct);
                }
                else
                {
                    _services.AddRange(config.Services);
                }

                foreach (var svc in _services)
                {
                    if (svc.Accounts is null)
                        svc.Accounts = new ObservableCollection<AIAccount>();
                }

                Sort();
                ServicesChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load AI services. Using defaults.");
                _services.Clear();
                _services.AddRange(CreateDefaults());
                Sort();
                ServicesChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public async Task SaveAsync(CancellationToken ct = default)
        {
            var config = new AppConfiguration
            {
                Version = CurrentConfigVersion,
                Services = _services.ToList()
            };
            await _storage.SaveAsync(config, ct);
        }

        public AIService AddService(AIService service)
        {
            if (string.IsNullOrWhiteSpace(service.Id))
                service.Id = Guid.NewGuid().ToString("N");

            if (service.Accounts is null)
                service.Accounts = new ObservableCollection<AIAccount>();

            if (service.Accounts.Count == 0)
            {
                service.Accounts.Add(new AIAccount
                {
                    ServiceId = service.Id,
                    Name = "Default",
                    Icon = service.Icon,
                    Accent = service.Accent,
                    IsDefault = true
                });
            }

            service.Order = _services.Count == 0 ? 0 : _services.Max(s => s.Order) + 1;
            _services.Add(service);
            Sort();
            ServicesChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
            return service;
        }

        public void UpdateService(AIService service)
        {
            var existing = _services.FirstOrDefault(s => s.Id == service.Id);
            if (existing is null) return;

            existing.Name = service.Name;
            existing.Url = service.Url;
            existing.Icon = service.Icon;
            existing.Accent = service.Accent;
            existing.Favorite = service.Favorite;
            existing.Pinned = service.Pinned;
            existing.Enabled = service.Enabled;
            existing.OpenInExternalBrowser = service.OpenInExternalBrowser;

            Sort();
            ServicesChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
        }

        public void RemoveService(string id)
        {
            var existing = _services.FirstOrDefault(s => s.Id == id);
            if (existing is null) return;

            _services.Remove(existing);
            Sort();
            ServicesChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
        }

        public void ToggleFavorite(string id)
        {
            var existing = _services.FirstOrDefault(s => s.Id == id);
            if (existing is null) return;

            existing.Favorite = !existing.Favorite;
            Sort();
            ServicesChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
        }

        // ═══════════════════════════════════════════════════════
        //  Reorder — smooth path (no Sort, no event, no flicker)
        // ═══════════════════════════════════════════════════════
        public void ReorderServices(IEnumerable<string> orderedIds)
        {
            var orderedList = orderedIds.ToList();
            var reordered = new List<AIService>(orderedList.Count);

            foreach (var id in orderedList)
            {
                var svc = _services.FirstOrDefault(s => s.Id == id);
                if (svc is not null)
                    reordered.Add(svc);
            }

            foreach (var svc in _services)
            {
                if (!reordered.Contains(svc))
                    reordered.Add(svc);
            }

            for (int i = 0; i < reordered.Count; i++)
            {
                reordered[i].Order = i;
            }

            _services.Clear();
            _services.AddRange(reordered);

            // Deliberately NO Sort() and NO ServicesChanged.
            _ = SaveAsync();
        }

        public AIService? FindService(string id)
            => _services.FirstOrDefault(s => s.Id == id);

        public void ResetToDefaults()
        {
            _services.Clear();
            _services.AddRange(CreateDefaults());
            Sort();
            ServicesChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
        }

        private void Sort()
        {
            var sorted = _services
                .OrderByDescending(s => s.Favorite)
                .ThenBy(s => s.Order)
                .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _services.Clear();
            _services.AddRange(sorted);
        }

        private static IEnumerable<AIService> CreateDefaults()
        {
            var list = new List<AIService>
            {
                new() { Name = "ChatGPT",          Url = "https://chatgpt.com/",           Icon = "\uE8F2", Accent = AccentColor.Purple, Order = 0 },
                new() { Name = "Gemini",           Url = "https://gemini.google.com/",     Icon = "\uE99A", Accent = AccentColor.Blue,   Order = 1 },
                new() { Name = "Claude",           Url = "https://claude.ai/",             Icon = "\uE8A5", Accent = AccentColor.Orange, Order = 2 },
                new() { Name = "Perplexity",       Url = "https://www.perplexity.ai/",     Icon = "\uE721", Accent = AccentColor.Cyan,   Order = 3 },
                new() { Name = "Grok",             Url = "https://grok.com/",              Icon = "\uE8F2", Accent = AccentColor.Blue,   Order = 4 },
                new() { Name = "DeepSeek",         Url = "https://chat.deepseek.com/",     Icon = "\uE99A", Accent = AccentColor.Purple, Order = 5 },
                new() { Name = "Copilot",          Url = "https://copilot.microsoft.com/", Icon = "\uE774", Accent = AccentColor.Green,  Order = 6 },
                new() { Name = "Poe",              Url = "https://poe.com/",               Icon = "\uE8F2", Accent = AccentColor.Purple, Order = 7 },
                new() { Name = "Mistral",          Url = "https://chat.mistral.ai/",       Icon = "\uE945", Accent = AccentColor.Orange, Order = 8 },
                new() { Name = "OpenRouter",       Url = "https://openrouter.ai/chat",     Icon = "\uE774", Accent = AccentColor.Cyan,   Order = 9 },
                new() { Name = "Google AI Studio", Url = "https://aistudio.google.com/",   Icon = "\uE99A", Accent = AccentColor.Blue,   Order = 10 },
                new() { Name = "Groq",             Url = "https://groq.com/",              Icon = "\uE945", Accent = AccentColor.Green,  Order = 11 },
                new() { Name = "Qwen",             Url = "https://chat.qwen.ai/",          Icon = "\uE99A", Accent = AccentColor.Purple, Order = 12 },
            };

            foreach (var svc in list)
            {
                svc.Accounts.Add(new AIAccount
                {
                    ServiceId = svc.Id,
                    Name = "Default",
                    Icon = svc.Icon,
                    Accent = svc.Accent,
                    IsDefault = true
                });
            }

            return list;
        }
    }
}