using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InstaSharper;
using InstaSharper.Classes;
using InstaSharper.API;
using InstaSharper.API.Builder;
using InstaSharper.Logger;
using InstaSharper.Classes.Models;

namespace instaBot
{
    class Program
    {
        #region Hidden
        private const string username = "robot.ima";
        private const string password = "Fubar865";
        #endregion

        private static UserSessionData user;
        private static IInstaApi api;
        public static DateTime Time = System.DateTime.Now;
        public static DateTime TargetTime = Time.AddHours(1);
        public static int CallCount = 0;

        public static List<InstaUserShort> FinalShortList = new List<InstaUserShort>();
        static void Main(string[] args)
        {
            user = new UserSessionData();
            user.UserName = username;
            user.Password = password;
            PrintUI("Attempting Login - " + Time);
            Login();
            Console.ReadKey(); //PrintUI(null);

            pullUserPosts("elonrmuskk");
            Console.ReadKey(); PrintUI(null);

            foreach (InstaUserShort user in FinalShortList)
            {
                Console.WriteLine("InList: "+user.UserName);
            }
            Console.WriteLine("Press 'Enter' to continue");
            Console.ReadKey(); PrintUI(null);

            FollowUsers();
            Console.ReadKey();
        }

        public static async void Login()
        {
            api = InstaApiBuilder.CreateBuilder()
                .SetUser(user)
                .UseLogger(new DebugLogger(LogLevel.Exceptions))
                .SetRequestDelay(RequestDelay.FromSeconds(8,8))
                .Build();

            var loginRequest = await api.LoginAsync(); CallCount++;

            if (loginRequest.Succeeded)
                PrintUI("Logged in\nPress 'Enter' to continue");
            else
                PrintUI("Error logging in\n" + loginRequest.Info.Message);
        }

        public static async void pullUserPosts(string username)
        {
            IResult<InstaUser> userSearch = await api.GetUserAsync(username); CallCount++;
            //Console.WriteLine($"User: {userSearch.Value.FullName} n\tFollowers: { userSearch.Value.FollowersCount} \n\tVerified:  { userSearch.Value.IsVerified}");

            IResult<InstaMediaList> media = await api.GetUserMediaAsync(username, PaginationParameters.MaxPagesToLoad(1)); CallCount++;

            List<InstaMedia> mediaList = media.Value.ToList();
            
            for(int i = 0; i < mediaList.Count; i++)
            {
                InstaMedia m = mediaList[i];
                if (i > 5)
                    break;
                if(m != null)
                {
                    PrintUI("media ID: " + m.InstaIdentifier);

                    IResult<InstaLikersList> listr = await api.GetMediaLikersAsync(m.InstaIdentifier); CallCount++;

                    PrintUI("Likes: " + m.LikesCount);
                    InstaUserShortList likersList = m.Likers;
                    foreach (InstaUserShort user in listr.Value.ToList())
                    {
                        if (!FinalShortList.Contains(user))
                        {
                            FinalShortList.Add(user);
                        }
                    }
                }
            }

            Console.WriteLine("Press 'Enter' to continue");
        }

        public static  void FollowUsers()
        {
            string userHandle = null;

            List<long> LongList = new List<long>();

            foreach (InstaUserShort shortUser in FinalShortList)
            {
                userHandle = shortUser.UserName.ToString();
                LongList.Add(Convert.ToInt64(shortUser.Pk));
                //get user's id
                Console.WriteLine("Queueing " + shortUser.UserName); 
            }

            foreach(long l in LongList)
            {
                if (CallCount >= 10)
                {
                    while (Time < TargetTime)
                    {
                        PrintUI("Call Limit Reached: " + CallCount + " -- Waiting for target time " + TargetTime);
                        System.Threading.Thread.Sleep(5000);
                        Time= System.DateTime.Now;
                        if (TargetTime <= Time)
                        {
                            CallCount = 0;
                            TargetTime.AddHours(1);
                        }
                    }
                }
                try
                {

                    var t = Task.Run(
                    async () =>
                    {
                        await api.FollowUserAsync(l); CallCount++;
                    });
                    t.Wait();
                    Console.WriteLine("Call succeeded for user: " + l);
                    //api.FollowUserAsync(l);
                } catch (Exception) { Console.WriteLine("Call Failed for user: " + l); }
                
                PrintUI("Followed " + l);
            }

            Console.WriteLine("Press 'Enter' to Exit");
        }

        public static async void FollowSingleUser(long PK)
        {
            IResult<InstaFriendshipStatus> eh = await api.FollowUserAsync(PK);
        }
        
        public static void PrintUI(string message)
        {
            Console.Clear();
            Console.WriteLine("Calls: "+CallCount+" -- Time: " + Time + "|| Target: "+TargetTime); Console.WriteLine();
            Console.WriteLine("============================================================"); Console.WriteLine();
            Console.WriteLine(message);
        }
    }
}
