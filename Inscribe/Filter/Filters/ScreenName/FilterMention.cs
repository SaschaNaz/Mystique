﻿using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Inscribe.Storage.Perpetuation;
using Inscribe.Text;

namespace Inscribe.Filter.Filters.ScreenName
{
    public class FilterMention : ScreenNameFilterBase
    {
        private FilterMention() { }

        public FilterMention(string needle)
        {
            this.needle = needle;
        }

        protected override bool FilterStatus(TweetBackEnd status)
        {
            return RegularExpressions.AtRegex.Matches(status.Text)
                .Cast<Match>().Any(m => Match(m.Groups[1].Value, needle));
        }

        public override string Identifier
        {
            get { return "mention"; }
        }

        public override IEnumerable<object> GetArgumentsForQueryify()
        {
            yield return needle;
        }

        public override string Description
        {
            get { return "指定ユーザーへの返信を含む"; }
        }

        public override string FilterStateString
        {
            get { return "@" + this.needle + " への返信"; }
        }
    }
}
