﻿using System;
using System.Threading;

namespace Inscribe.Data
{
    /// <summary>
    /// 外部に対してReaderWriterLockを提供します。<para />
    /// 使い方に気を付けてね☆
    /// </summary>
    public class ReaderWriterLockWrap : ReaderWriterLockBase
    {
        public ReaderWriterLockWrap() : this(LockRecursionPolicy.SupportsRecursion) { }
        public ReaderWriterLockWrap(LockRecursionPolicy lrp) : base(lrp) { }

        public IDisposable GetReaderLock()
        {
            return ReaderLock();
        }

        public IDisposable GetWriterLock()
        {
            return WriterLock();
        }

        public IDisposable GetUpgradableReaderLock()
        {
            return UpgradableReaderLock();
        }

        public string WriteDown()
        {
            return
                "READING:" + base.readerWriterLock.CurrentReadCount + "/" +
                "WAITREAD:" + base.readerWriterLock.WaitingReadCount + "/" +
                "WAITUPGR:" + base.readerWriterLock.WaitingUpgradeCount + "/" +
                "WAITWRIT" + base.readerWriterLock.WaitingWriteCount + "/";
        }
    }
}
