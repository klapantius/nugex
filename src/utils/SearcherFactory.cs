namespace nugex.utils
{
    public static class SearcherFactory
    {
        internal static ISearcher Mock { get; set; } = null;
        public static ISearcher Create() => Mock ?? new Searcher();
    }
}