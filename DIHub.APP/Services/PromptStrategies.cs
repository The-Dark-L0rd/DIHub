using System.Text.Json;

namespace DIHub.APP.Services
{
    public interface IPromptStrategy
    {
        string Name { get; }
        bool CanHandle(string url);
        string[] InputSelectors { get; }
        string[] SendSelectors { get; }

        // Added to the interface so PromptDispatcher can call them:
        string BuildTypeScript(string prompt);
        string BuildSendScript();
    }

    public abstract class PromptStrategyBase : IPromptStrategy
    {
        public abstract string Name { get; }
        public abstract bool CanHandle(string url);
        protected abstract string[] Inputs { get; }
        protected abstract string[] Sends { get; }

        public string[] InputSelectors => Inputs;
        public string[] SendSelectors => Sends;

        private string InputsJson => JsonSerializer.Serialize(Inputs);
        private string SendsJson => JsonSerializer.Serialize(Sends);

        public string BuildTypeScript(string prompt)
        {
            var jsonPrompt = JsonSerializer.Serialize(prompt);

            return @"
(function() {
    const text = " + jsonPrompt + @";

    function isVisible(el) {
        if (!el) return false;
        const r = el.getBoundingClientRect();
        if (r.width === 0 || r.height === 0) return false;
        const s = window.getComputedStyle(el);
        if (s.display === 'none' || s.visibility === 'hidden') return false;
        if (s.opacity === '0') return false;
        return true;
    }

    function setText(el, t) {
        if (el.tagName === 'TEXTAREA' || el.tagName === 'INPUT') {
            const proto = el.tagName === 'TEXTAREA'
                ? HTMLTextAreaElement.prototype
                : HTMLInputElement.prototype;
            const setter = Object.getOwnPropertyDescriptor(proto, 'value').set;
            setter.call(el, t);
            el.dispatchEvent(new Event('input', { bubbles: true }));
            el.dispatchEvent(new Event('change', { bubbles: true }));
            return true;
        }
        if (el.isContentEditable) {
            el.focus();
            const range = document.createRange();
            range.selectNodeContents(el);
            const sel = window.getSelection();
            sel.removeAllRanges();
            sel.addRange(range);
            const ok = document.execCommand('insertText', false, t);
            if (!ok) {
                el.textContent = t;
                el.dispatchEvent(new InputEvent('input', { bubbles: true, data: t, inputType: 'insertText' }));
            }
            return true;
        }
        return false;
    }

    const selectors = " + InputsJson + @";
    let input = null;
    for (const sel of selectors) {
        for (const el of document.querySelectorAll(sel)) {
            if (isVisible(el)) { input = el; break; }
        }
        if (input) break;
    }

    if (!input) return JSON.stringify({ ok: false, reason: 'no-input' });
    if (!setText(input, text)) return JSON.stringify({ ok: false, reason: 'set-failed' });

    return JSON.stringify({ ok: true, tag: input.tagName });
})();
";
        }

        public string BuildSendScript()
        {
            return @"
(function() {
    function isVisible(el) {
        if (!el) return false;
        const r = el.getBoundingClientRect();
        if (r.width === 0 || r.height === 0) return false;
        const s = window.getComputedStyle(el);
        if (s.display === 'none' || s.visibility === 'hidden') return false;
        return true;
    }

    const selectors = " + SendsJson + @";
    for (const sel of selectors) {
        for (const btn of document.querySelectorAll(sel)) {
            if (isVisible(btn) && !btn.disabled) {
                btn.click();
                return JSON.stringify({ ok: true, tag: btn.tagName });
            }
        }
    }

    const active = document.activeElement;
    if (active && active !== document.body) {
        const ev = new KeyboardEvent('keydown', {
            key: 'Enter', code: 'Enter', keyCode: 13, which: 13,
            bubbles: true, cancelable: true
        });
        active.dispatchEvent(ev);
        const ev2 = new KeyboardEvent('keyup', {
            key: 'Enter', code: 'Enter', keyCode: 13, which: 13,
            bubbles: true, cancelable: true
        });
        active.dispatchEvent(ev2);
        return JSON.stringify({ ok: true, method: 'enter' });
    }

    return JSON.stringify({ ok: false, reason: 'no-send' });
})();
";
        }
    }

    public sealed class ChatGPTStrategy : PromptStrategyBase
    {
        public override string Name => "ChatGPT";
        public override bool CanHandle(string url) =>
            url.Contains("chatgpt.com", System.StringComparison.OrdinalIgnoreCase) ||
            url.Contains("chat.openai.com", System.StringComparison.OrdinalIgnoreCase);

        protected override string[] Inputs => new[]
        {
            "#prompt-textarea",
            "textarea[data-id]",
            "div.ProseMirror[contenteditable='true']",
            "div[contenteditable='true'][role='textbox']",
            "textarea"
        };

        protected override string[] Sends => new[]
        {
            "button[data-testid='send-button']",
            "button[aria-label='Send prompt']",
            "button[aria-label*='Send']",
            "#composer-submit-button",
            "button[type='submit']"
        };
    }

    public sealed class ClaudeStrategy : PromptStrategyBase
    {
        public override string Name => "Claude";
        public override bool CanHandle(string url) =>
            url.Contains("claude.ai", System.StringComparison.OrdinalIgnoreCase);

        protected override string[] Inputs => new[]
        {
            "div.ProseMirror[contenteditable='true']",
            "div[contenteditable='true'][role='textbox']",
            "div[contenteditable='true']",
            "textarea"
        };

        protected override string[] Sends => new[]
        {
            "button[aria-label='Send message']",
            "button[aria-label*='Send']",
            "button[data-testid='send-button']",
            "button[type='submit']"
        };
    }

    public sealed class GeminiStrategy : PromptStrategyBase
    {
        public override string Name => "Gemini";
        public override bool CanHandle(string url) =>
            url.Contains("gemini.google.com", System.StringComparison.OrdinalIgnoreCase) ||
            url.Contains("bard.google.com", System.StringComparison.OrdinalIgnoreCase);

        protected override string[] Inputs => new[]
        {
            "div.ql-editor[contenteditable='true']",
            "rich-textarea div[contenteditable='true']",
            "div[contenteditable='true'][role='textbox']",
            "textarea"
        };

        protected override string[] Sends => new[]
        {
            "button.send-button",
            "button[aria-label*='Send']",
            "button[aria-label='Send message']",
            "button[type='submit']"
        };
    }

    public sealed class GenericPromptStrategy : PromptStrategyBase
    {
        public override string Name => "Generic";
        public override bool CanHandle(string url) => true;

        protected override string[] Inputs => new[]
        {
            "textarea[placeholder]",
            "textarea",
            "div[contenteditable='true'][role='textbox']",
            "div[contenteditable='true']",
            "[role='textbox']"
        };

        protected override string[] Sends => new[]
        {
            "button[type='submit']",
            "button[aria-label*='Send']",
            "button[title*='Send']",
            "button[data-testid*='send']"
        };
    }
}