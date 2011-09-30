﻿using System;
using Inscribe.Filter.Core;
using Inscribe.Storage.Perpetuation;

namespace Inscribe.Filter
{
    public interface  IFilter : IQueryConvertable
    {
        /// <summary>
        /// フィルタを適用します。<para />
        /// Negate値は考慮されます。
        /// </summary>
        /// <returns>フィルタを通過したか</returns>
        bool Filter(TweetBackend backend);

        /// <summary>
        /// 否定条件であるか
        /// </summary>
        bool Negate { get; set; }

        /// <summary>
        /// 現在適用中のフィルタを破棄し、フィルタを再適用するように要求されました。
        /// </summary>
        event Action RequireReaccept;

        /// <summary>
        /// 特定のステータスについてフィルタに再度通すように要求されました。
        /// </summary>
        event Action<long> RequirePartialReaccept;
    }
}
