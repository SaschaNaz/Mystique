﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using Inscribe.Common;
using Inscribe.ViewModels.PartBlocks.MainBlock.TimelineChild;

namespace Mystique.Views.Converters.Particular
{
    public class SourceToInlinesConverter : OneWayConverter<TweetViewModel, IEnumerable<Inline>>
    {
        static Regex SourceRegex = new Regex("=\"(.*?)\".*?>(.*?)<", RegexOptions.Singleline | RegexOptions.Compiled);

        public override IEnumerable<Inline> ToTarget(TweetViewModel input, object parameter)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                return Application.Current.Dispatcher.Invoke(new Action(() => ToTarget(input, parameter)), null) as IEnumerable<Inline>;

            var src = input.BackEnd.Source;
            Match m = null;
            if (!String.IsNullOrEmpty(src) && (m = SourceRegex.Match(src.Replace("\\", ""))).Success)
            {
                var hlink = new Hyperlink(new Run(m.Groups[2].Value));
                hlink.Click += (o, e) => Browser.Start(m.Groups[1].Value);
                hlink.Foreground = Brushes.DodgerBlue;
                return new[] { hlink };
            }
            else
            {
                return new[] { new Run(input.BackEnd.Source) };
            }
        }
    }
}
