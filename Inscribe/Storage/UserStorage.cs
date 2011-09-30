﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Dulcet.Twitter;
using Dulcet.Twitter.Rest;
using Inscribe.Data;
using Inscribe.Storage.Perpetuation;
using Inscribe.ViewModels.PartBlocks.MainBlock;

namespace Inscribe.Storage
{
    public static class UserStorage
    {
        static ReaderWriterLockWrap lockWrap = new ReaderWriterLockWrap(LockRecursionPolicy.NoRecursion);

        static Dictionary<long, UserViewModel> dictionary = new Dictionary<long, UserViewModel>();

        /// <summary>
        /// キャッシュにユーザー情報が存在していたら、すぐに返します。<para />
        /// キャッシュに存在しない場合はNULLを返します。
        /// </summary>
        public static UserViewModel Lookup(string userScreenName)
        {
            if (userScreenName == null)
                throw new ArgumentNullException("userScreenName");
            using (lockWrap.GetReaderLock())
            {
                return dictionary.Values.Where(u => u.Backend.ScreenName == userScreenName).FirstOrDefault();
            }
        }

        public static UserViewModel Lookup(long id)
        {
            UserViewModel ret;
            return dictionary.TryGetValue(id, out ret) ? ret : null;
        }

        /// <summary>
        /// User ViewModelを生成して、キャッシュに追加します。
        /// </summary>
        public static void Register(TwitterUser user)
        {
            Get(user);
        }

        /// <summary>
        /// User ViewModelを取得します。<para />
        /// 内部キャッシュを更新します。
        /// </summary>
        /// <param name="user">ユーザー情報(nullは指定できません)</param>
        public static UserViewModel Get(TwitterUser user)
        {
            if (user == null)
                throw new ArgumentNullException("user");
            var newvm = new UserViewModel(new UserBackend(user));
            using (lockWrap.GetWriterLock())
            {
                if (dictionary.ContainsKey(user.NumericId))
                    dictionary[user.NumericId] = newvm;
                else
                    dictionary.Add(user.NumericId, newvm);
            }
            return newvm;
        }

        /// <summary>
        /// User ViewModelを取得します。<para />
        /// nullを返すことがあります。
        /// </summary>
        /// <param name="userScreenName">ユーザースクリーン名</param>
        /// <param name="useCache">内部キャッシュが可能であれば使用する</param>
        /// <returns></returns>
        public static UserViewModel Get(string userScreenName, bool useCache = true)
        {
            if (String.IsNullOrEmpty(userScreenName))
                throw new ArgumentNullException("userScreenName", "userScreenNameがNullであるか、または空白です。");
            UserViewModel ret = null;
            if (useCache && (ret = Lookup(userScreenName)) != null)
            {
                return ret;
            }
            else
            {
                var acc = AccountStorage.GetRandom(ai => true, true);
                if (acc != null)
                {
                    try
                    {
                        var ud = acc.GetUserByScreenName(userScreenName);
                        if (ud != null)
                        {
                            var uvm = new UserViewModel(new UserBackend(ud));
                            using (lockWrap.GetWriterLock())
                            {
                                dictionary.Add(ud.NumericId, uvm);
                            }
                            return uvm;
                        }
                    }
                    catch (Exception e)
                    {
                        ExceptionStorage.Register(e, ExceptionCategory.TwitterError, "ユーザー情報の受信に失敗しました。(ユーザー @" + userScreenName + " を アカウント @" + acc.ScreenName + " で受信しようとしました。)");
                    }
                    return null;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// ストレージに格納されているすべてのユーザーを取得します。
        /// </summary>
        /// <returns></returns>
        public static UserViewModel[] GetAll()
        {
            using (lockWrap.GetReaderLock())
            {
                return dictionary.Values.ToArray();
            }
        }
    }
}
