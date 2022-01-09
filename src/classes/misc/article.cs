namespace merlin.classes
{
    public class Article
    {
        public string Title { get; }
        public string Url { get; }

        public Article(string title, string url)
        {
            Title = title;
            Url = url;
        }
    }
}