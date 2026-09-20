using System.Threading.Tasks;

namespace DIHub.Core.Interfaces
{
    public interface IBrowserService
    {
        void OpenInDefaultBrowser(string url);
        void OpenInBrowser(string url, ExternalBrowser browser);
        bool IsValidWebUrl(string url);

        string BuildSearchUrl(string query, SearchEngine engine = SearchEngine.Google);
        string NormalizeUrlOrSearch(string input, SearchEngine engine = SearchEngine.Google);
    }

    public enum ExternalBrowser { Default, Chrome, Edge }
    public enum SearchEngine { Google, Bing, DuckDuckGo }
    public enum OpenLinksBehavior { CurrentTab, NewTab, ExternalBrowser }
}