﻿
using Inscribe.Storage.Perpetuation;
namespace Inscribe.Filter.Filters.Text
{
    public class FilterText : TextFilterBase
    {
        private FilterText() { }

        public FilterText(string needle) : this(needle, false) { }

        public FilterText(string needle, bool isCaseSensitive)
        {
            this.needle = needle;
            this.isCaseSensitive = isCaseSensitive;
        }

        protected override bool FilterStatus(TweetBackend status)
        {
            return this.Match(status.Text, this.needle, this.isCaseSensitive);
        }

        public override string Identifier
        {
            get { return "text"; }
        }

        public override string Description
        {
            get { return "本文"; }
        }
    }
}
