namespace MoogleEngine;

public class SearchItem
{
    public SearchItem(string title, List<string> snippet, double score, string url)
    {
        this.Title = title;
        this.Snippet = snippet;
        this.Score = score;
        this.URL = url;
    }

    public string Title { get; private set; }

    public List<string> Snippet { get; private set; }

    public double Score { get; private set; }

    public string URL { get; private set; }
}
