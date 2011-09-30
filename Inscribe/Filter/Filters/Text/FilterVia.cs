﻿using Inscribe.Storage.Perpetuation;

namespace Inscribe.Filter.Filters.Text
{
    public class FilterVia : TextFilterBase
    {
        private FilterVia() { }

        public FilterVia(string needle) : this(needle, false) { }

        public FilterVia(string needle, bool isCaseSensitive)
        {
            this.needle = needle;
            this.isCaseSensitive = isCaseSensitive;
        }

        protected override bool FilterStatus(TweetBackend status)
        {
            return !status.IsDirectMessage && this.Match(status.Source, this.needle, this.isCaseSensitive);
        }

        public override string Identifier
        {
            get { return "via"; }
        }

        public override string Description
        {
            get { return "クライアント名"; }
        }
    }
}
