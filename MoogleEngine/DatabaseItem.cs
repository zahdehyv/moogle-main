namespace MoogleEngine;

public class DatabaseItem
{
    public DatabaseItem(List<string> files, Dictionary<string,int> words, string[] wordlist, List<List<string>> files_words, double[,] tfidf)
    {
        this.Files = files;
        this.Words = words;
        this.WordsList = wordlist;
        this.FilesWords = files_words;
        this.TfIdf = tfidf;
    }

    public List<string> Files { get; private set; }

    public Dictionary<string,int> Words { get; private set; }

    public string[] WordsList { get; private set; }

    public List<List<string>> FilesWords { get; private set; }
    
    public double[,] TfIdf { get; private set; }
}
