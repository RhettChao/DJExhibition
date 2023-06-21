using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using Exhibition.Models;

using System.Threading;
using System.Threading.Tasks;
using System.Configuration;

using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Mvc;
using Google.Apis.Util.Store;

namespace Exhibition.Controllers
{
    public class GoogleCalController : Controller
    {
        static string SuhuaCalendarID = "apps.sinotech.com.tw_jp4ev4hrlo4ktdvmi4oh9ht758@group.calendar.google.com";
        //static string SuhuaCalendarOwner = "suhua-a@apps.sinotech.com.tw";

        //StateCode:
        //10-送出申請 20-已認證 25-已核准 30-已取消
        static int StateCode_New = 10;
        static int StateCode_Verify = 20;
        //static int StateCode_Auth = 25;
        static int StateCode_Cancel = 30;

        // GET: GoogleCal
        public async Task<ActionResult> Index(CancellationToken cancellationToken)
        {
            if (User.Identity.IsAuthenticated)
            {
                ViewBag.Msg = User.Identity.Name;                
                //Session["user"] = User.Identity.Name;

                var result = await new AuthorizationCodeMvcApp(this, new AppFlowMetadata()).
                    AuthorizeAsync(cancellationToken);

                if (result.Credential != null)
                {
                    //Session["Credential"] = result.Credential;

                    var service = new CalendarService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = result.Credential,
                        ApplicationName = "TestCalendar",
                    });

                    var list = service.CalendarList.List().Execute().Items;
                    var readerList = list.Where(i => i.AccessRole == "reader");

                    //List<CalendarListEntry> myList = new List<CalendarListEntry>();
                    //foreach (CalendarListEntry item in list) { 
                    //    item.    
                    //}

                    var suhuaList = list.Where(l => l.Id == SuhuaCalendarID).FirstOrDefault();
                    if (suhuaList != null && suhuaList.Selected != null && suhuaList.Selected.Value)
                    {
                        ViewBag.Status = "已訂閱";
                    }
                    else {
                        ViewBag.Status = "未訂閱";
                    }


                    return View(readerList);
                    //return Json(list, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    //Session["Credential"] = null;
                    return new RedirectResult(result.RedirectUri);
                }
            }
            else
            {
                //ViewBag.Msg = "請先登入!";
                //return View(new List<string>());
                return Redirect("~/");
            }
        }            
        
        public async Task<ActionResult> Subscribe(CancellationToken cancellationToken)
        {
            if (User.Identity.IsAuthenticated)
            {
                //ViewBag.Msg = User.Identity.Name;
                //Session["user"] = "user_001";
                //Session["user"] = User.Identity.Name;

                var result = await new AuthorizationCodeMvcApp(this, new AppFlowMetadata()).
                    AuthorizeAsync(cancellationToken);

                if (result.Credential != null)
                {
                    //Session["Credential"] = result.Credential;

                    var service = new CalendarService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = result.Credential,
                        ApplicationName = "TestCalendar",
                    });

                    //Calendar cal = new Calendar();
                    //cal.Id = "apps.sinotech.com.tw_jp4ev4hrlo4ktdvmi4oh9ht758@group.calendar.google.com";
                    //service.Calendars.Insert(cal).Execute();

                    var suhuaList = service.CalendarList.List().Execute().Items.Where(l => l.Id == SuhuaCalendarID).FirstOrDefault();

                    if (suhuaList == null)
                    {
                        CalendarListEntry e = new CalendarListEntry();
                        e.Id = SuhuaCalendarID;
                        service.CalendarList.Insert(e).Execute();
                    }
                    else {
                        if (suhuaList.Selected == null || !suhuaList.Selected.Value)
                        {
                            suhuaList.Selected = true;
                            service.CalendarList.Update(suhuaList, SuhuaCalendarID).Execute();
                        }
                        else {
                            return Json(0, JsonRequestBehavior.AllowGet);
                        }
                    }

                    return Json(1, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    //Session["Credential"] = null;
                    return new RedirectResult(result.RedirectUri);
                }
            }
            else
            {
                //ViewBag.Msg = "請先登入!";
                //return View(new List<string>());
                return Json(-1, JsonRequestBehavior.AllowGet);
            }
        }

        public async Task<ActionResult> Unsubscribe(CancellationToken cancellationToken)
        {
            if (User.Identity.IsAuthenticated)
            {
                //ViewBag.Msg = User.Identity.Name;
                //Session["user"] = "user_001";
                //Session["user"] = User.Identity.Name;

                var result = await new AuthorizationCodeMvcApp(this, new AppFlowMetadata()).
                    AuthorizeAsync(cancellationToken);

                if (result.Credential != null)
                {
                    //Session["Credential"] = result.Credential;

                    var service = new CalendarService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = result.Credential,
                        ApplicationName = "TestCalendar",
                    });

                    var suhuaList = service.CalendarList.List().Execute().Items.Where(l => l.Id == SuhuaCalendarID).FirstOrDefault();

                    if (suhuaList != null && suhuaList.Selected != null && suhuaList.Selected.Value)
                    {
                        suhuaList.Selected = false;
                        service.CalendarList.Update(suhuaList, SuhuaCalendarID).Execute();
                    }
                    else {
                        return Json(0, JsonRequestBehavior.AllowGet);
                    }

                    return Json(1, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    //Session["Credential"] = null;
                    return new RedirectResult(result.RedirectUri);
                }
            }
            else
            {
                //ViewBag.Msg = "請先登入!";
                //return View(new List<string>());
                return Json(-1, JsonRequestBehavior.AllowGet);
            }
        }

        public async Task<ActionResult> AddEvent(CancellationToken cancellationToken, string type, Guid id)
        {
            //Session["GoogleUser"] = SuhuaCalendarOwner;

            var result = await new AuthorizationCodeMvcApp(this, new AppFlowMetadata()).
                AuthorizeAsync(cancellationToken);

            if (result.Credential != null)
            {
                //Session["GoogleUser"] = null;
                //Session["Credential"] = result.Credential;                
                var service = new CalendarService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = result.Credential,
                    ApplicationName = "TestCalendar",
                });

                //var list = service.CalendarList.List().Execute().Items;
                //return View(list);

                DatabaseEntities db = new DatabaseEntities();
                Reservation res = db.Reservation.Where(r => r.ID == id).FirstOrDefault();
                string eventID = "";
                //if (res != null && type == "auth" && res.StateCode.Value == StateCode_Verify && res.AuthDate != null)
                //有核准通知後啟動行事曆記錄
                if (res != null && type == "auth" && res.AuthDate != null)
                {
                    bool revised = false;
                    Event e = null;
                    //var suhuaList = service.CalendarList.List().Execute().Items.Where(l => l.Id == SuhuaCalendarID).FirstOrDefault();
                    var events = service.Events.List(SuhuaCalendarID).Execute().Items;
                    foreach (Event lastEvent in events)
                    {
                        if (lastEvent.Description != null && lastEvent.Description.Contains('：'))
                        {
                            //string lastID = lastEvent.Description.Split('：')[1];
                            string lastID = lastEvent.Description.Split('\n')[0].Split('：')[1];
                            if (lastEvent.Summary.StartsWith("願景館預約：") && res.SN.ToString() == lastID)
                            {
                                e = lastEvent;
                                revised = true;
                                break;
                            }
                        }
                    }

                    DateTime start = res.ReservationBegin.Value;
                    DateTime end = res.ReservationEnd.Value;
                    if (e == null)
                        e = new Event();                    
                    e.Summary = "願景館預約：" + res.GroupName + "(預約人數：" + res.TouristRange1 + "人)";
                    e.Description = "預約代碼：" + res.SN + "\n聯 絡 人：" + res.ContactName + "\n行動電話：" + res.ContactMobile + "\n電子郵件：" + res.ContactEmail + "\n備註：" + res.ContactNote;
                    e.Start = new EventDateTime()
                    {
                        DateTime = start
                    };
                    e.End = new EventDateTime()
                    {
                        DateTime = end
                    };

                    if (revised)
                    {
                        eventID = service.Events.Update(e, SuhuaCalendarID, e.Id).Execute().Id;
                    }
                    else
                    {
                        eventID = service.Events.Insert(e, SuhuaCalendarID).Execute().Id;
                    }
                }

                //Events evs = service.Events.List(calendarID).Execute();
                //for (int i = 0; i < evs.Items.Count; i++) {
                //    if (evs.Items[i].Summary.Contains("XXX")) {
                //        eventID = service.Events.Delete(calendarID, evs.Items[i].Id).Execute();
                //        continue;
                //    }
                //}

                return Json(eventID, JsonRequestBehavior.AllowGet);
            }
            else
            {
                //Session["Credential"] = null;                
                //return new RedirectResult(result.RedirectUri);
                //Session["GoogleUser"] = null;
                return Json(-1, JsonRequestBehavior.AllowGet);

            }
        }

        public async Task<ActionResult> DeleteEvent(CancellationToken cancellationToken, Guid id)
        {
            //Session["GoogleUser"] = SuhuaCalendarOwner;

            var result = await new AuthorizationCodeMvcApp(this, new AppFlowMetadata()).
                AuthorizeAsync(cancellationToken);

            if (result.Credential != null)
            {
                //Session["GoogleUser"] = null;
                //Session["Credential"] = result.Credential;

                var service = new CalendarService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = result.Credential,
                    ApplicationName = "TestCalendar",
                });
                DatabaseEntities db = new DatabaseEntities();
                Reservation res = db.Reservation.Where(r => r.ID == id).FirstOrDefault();
                //string eventID = "";
                int deletdNo = 0;
                if (res != null)
                {
                    var evs = service.Events.List(SuhuaCalendarID).Execute().Items;
                    foreach (Event lastEvent in evs)
                    {
                        if (lastEvent.Description != null && lastEvent.Description.Contains('：'))
                        {
                            //string lastID = lastEvent.Description.Split('：')[1];
                            string lastID = lastEvent.Description.Split('\n')[0].Split('：')[1];
                            if (res.SN.ToString() == lastID)
                            {
                                service.Events.Delete(SuhuaCalendarID, lastEvent.Id).Execute();
                                deletdNo++;
                            }
                        }
                        //if (e.Summary.Contains("XXX"))
                        //{
                        //    eventID = service.Events.Delete(SuhuaCalendarID, e.Id).Execute();
                        //    continue;
                        //}
                    }

                }
                return Json(deletdNo, JsonRequestBehavior.AllowGet);
            }
            else
            {
                //Session["Credential"] = null;                
                //return new RedirectResult(result.RedirectUri);
                //Session["GoogleUser"] = null;
                return Json(-1, JsonRequestBehavior.AllowGet);

            }
        }
    }



    public class AuthCallbackController : Google.Apis.Auth.OAuth2.Mvc.Controllers.AuthCallbackController
    {
        protected override Google.Apis.Auth.OAuth2.Mvc.FlowMetadata FlowData
        {
            get { return new AppFlowMetadata(); }
        }
    }

    public class AppFlowMetadata : FlowMetadata
    {
        //static string GoogleClientId = "40921446753-3nenfr44ocrleg0vjvli2gtg8i7sme03.apps.googleusercontent.com";
        //static string GoogleClientSecret = "-ZCPJa7hu1xLAvFY0BCCfXLF";
        static string GoogleClientId = "705072874350-0mng5ruv03ebfjimq766d6h8r93t78v6.apps.googleusercontent.com";
        static string GoogleClientSecret = "xoqU83KkcunNYaa4oLjgu97k";
        //static string GoogleAuthCallback = @"/AuthCallback/IndexAsync";
        static string GoogleAuthCallback = ConfigurationManager.AppSettings["GoogleAuthCallback"];
        //static string GoogleAuthCallback = @"/Exhibition/AuthCallback/IndexAsync";
        static string SuhuaCalendarOwner = "suhua-a@apps.sinotech.com.tw";

        private static readonly IAuthorizationCodeFlow flow =
            new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = GoogleClientId,
                    ClientSecret = GoogleClientSecret,
                },
                Scopes = new[] { CalendarService.Scope.Calendar },
                //DataStore = new FileDataStore("D:\\Google.Apis.Auth", true),
                DataStore = new FileDataStore(ConfigurationManager.AppSettings["GoogleAuthDataStore"], true),
            });

        public override string GetUserId(Controller controller)
        {
            
            // In this sample we use the session to store the user identifiers.
            // That's not the best practice, because you should have a logic to identify
            // a user. You might want to use "OpenID Connect".
            // You can read more about the protocol in the following link:
            // https://developers.google.com/accounts/docs/OAuth2Login.            

            //var user = controller.Session["user"];

            string user = controller.HttpContext.User.Identity.Name;
            //if (controller.Request.Path == "/GoogleCal/AddEvent" || controller.Request.Path == "/GoogleCal/DeleteEvent") {
            if (controller.Request.AppRelativeCurrentExecutionFilePath == "~/GoogleCal/AddEvent" || controller.Request.AppRelativeCurrentExecutionFilePath == "~/GoogleCal/DeleteEvent")
            {
                
                user = SuhuaCalendarOwner;
            }


            //if (controller.Session["GoogleUser"] != null)
            //    user = controller.Session["GoogleUser"].ToString();

            //if (user == null)
            //{
            //    user = Guid.NewGuid();
            //    controller.Session["user"] = user;
            //}
            return user;

        }

        public override IAuthorizationCodeFlow Flow
        {
            get { return flow; }
        }

        /// <summary>
        /// Gets the authorization application's call back. That relative URL will handle the authorization code 
        /// response.
        /// </summary>
        public override string AuthCallback
        {
            get { return GoogleAuthCallback; }
            //get { return @"/AuthCallback/IndexAsync"; } //for localhost
            //get { return @"/MvcAuth/AuthCallback/IndexAsync"; } //for real website
        }
    }
}