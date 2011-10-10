﻿using System;
using System.Threading.Tasks;
using System.Windows;
using Inscribe.Common;
using Inscribe.Communication.UserStreams;
using Inscribe.Configuration;
using Inscribe.Plugin;
using Inscribe.Storage;
using Inscribe.Storage.Perpetuation;
using Inscribe.Subsystems;

namespace Inscribe.Core
{
    public static class Initializer
    {
        private static bool initialized = false;

        /// <summary>
        /// ウィンドウが表示されるより前に行われる初期化処理
        /// </summary>
        public static void Init()
        {
            if (initialized)
                throw new InvalidOperationException("アプリケーションは既に初期化されています。");
            initialized = true;

            // ネットワーク初期化
            Dulcet.Network.Http.Expect100Continue = false;
            Dulcet.Network.Http.MaxConnectionLimit = Int32.MaxValue;
            Dulcet.Network.Http.TimeoutInterval = 8000;

            // 設定のロード
            Setting.Initialize();

            // データベース初期化
            PerpetuationStorage.ConnectDB();

            // データベースとメモリストレージの同期
            if (Setting.Instance.KernelProperty.TweetPerpetuation)
                PerpetuationStorage.Writeback();

            // サブシステムの初期化
            NotificationCore.Initialize();
            HashtagStorage.Initialize();

            var apppath = System.IO.Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            // アップデータの存在を確認
            var updater = System.IO.Path.Combine(apppath, Define.UpdateFileName);
            if (System.IO.File.Exists(updater))
            {
                // .completeファイルが存在するか
                if (System.IO.File.Exists(updater + ".completed"))
                {
                    Action deleteAction = null;
                    deleteAction = new Action(() =>
                        {
                            try
                            {
                                // アップデータを削除
                                System.IO.File.Delete(updater);
                                System.IO.File.Delete(updater + ".completed");
                            }
                            catch (Exception e)
                            {
                                ExceptionStorage.Register(e, ExceptionCategory.AssertionFailed, "アップデータファイルの削除ができませんでした。", () => deleteAction());
                            }
                        });
                    deleteAction();
                }
                else
                {
                    // アップデータを起動して終了
                    UpdateReceiver.StartUpdateArchive();
                    return;
                }
            }

            // プラグインのロード
            PluginLoader.Load();

            UpdateReceiver.StartSchedule();
            Application.Current.Exit += new ExitEventHandler(AppExit);
        }

        private static bool standby = false;

        /// <summary>
        /// ウィンドウが表示された後に行われる初期化処理
        /// </summary>
        public static void StandbyApp()
        {
            KeyAssignCore.ReloadAssign();
            if (standby)
                throw new InvalidOperationException("既にアプリケーションはスタンバイ状態を経ました。");
            standby = true;
            Task.Factory.StartNew(() => Inscribe.Communication.CruiseControl.AutoCruiseSchedulerManager.Begin());
            Task.Factory.StartNew(() => ConnectionManager.RefreshReceivers());
            var call = OnStandbyApp;
            if (call != null)
                call();
            if (Environment.OSVersion.Version.Major < 6)
                NotifyStorage.Notify("ご利用のオペレーティングシステムはKrileでサポートされていません。");
        }

        public static event Action OnStandbyApp;

        static void AppExit(object sender, ExitEventArgs e)
        {
            Setting.Instance.Save();
        }
    }
}
