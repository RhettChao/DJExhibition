using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

using Exhibition.Models;
using System.IO;
using Microsoft.AspNet.Identity;

namespace Exhibition
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        private void Application_Error(object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError();
            using (StreamWriter sw = new StreamWriter(Server.MapPath("~/GlobalError.log"), true))
            {
                string Message = "";

                Message = "發生錯誤的網頁:{0}錯誤訊息:{1}堆疊內容:{2}";
                Message = String.Format(Message, Request.Path + Environment.NewLine, ex.GetBaseException().Message + Environment.NewLine, Environment.NewLine + ex.StackTrace);
                //Server.ClearError();
                sw.WriteLine(User.Identity.Name + " , " + DateTime.Now.ToString());
                sw.Write(Message);

                sw.WriteLine();
                sw.Close();
            }


            DatabaseEntities entity = new DatabaseEntities();
            ErrorLog log = new ErrorLog();
            log.Path = Request.Path;
            if (ex.GetBaseException().Message.Length > 500)
                log.Message = ex.GetBaseException().Message.Substring(0, 500);
            else
                log.Message = ex.GetBaseException().Message;
            if (ex.StackTrace.Length > 4000)
                log.Stack = ex.StackTrace.Substring(0, 4000);
            else
                log.Stack = ex.StackTrace;
            if (User.Identity.IsAuthenticated)
                log.UserName = User.Identity.Name;
            log.ErrorDate = DateTime.Now;
            entity.ErrorLog.Add(log);
            entity.SaveChanges();            
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            if (Request.Url.Host == "127.0.0.1" || Request.Url.Host == "localhost" || Request.Url.Host == "::1")
                return;

            DatabaseEntities entity = new DatabaseEntities();
            RequestLog log = new RequestLog();
            log.Path = Request.Path;
            log.UserHost = Request.UserHostAddress;
            //if (id > 0)
            //    log.UserId = id;
            if (Request.UserAgent.Length > 200)
                log.UserAgent = Request.UserAgent.Substring(0, 200);
            else
                log.UserAgent = Request.UserAgent;
            log.RequestDate = DateTime.Now;
            entity.RequestLog.Add(log);
            entity.SaveChanges();
        }

        void Application_AuthenticateRequest(object sender, EventArgs e)
        {
            if (Request.IsAuthenticated)
            {
                //// 先取得該使用者的 FormsIdentity
                //FormsIdentity id = (FormsIdentity)User.Identity;                
                //// 再取出使用者的 FormsAuthenticationTicket
                //FormsAuthenticationTicket ticket = id.Ticket;
                //// 將儲存在 FormsAuthenticationTicket 中的角色定義取出，並轉成字串陣列
                //string[] roles = ticket.UserData.Split(new char[] { ',' });
                //// 指派角色到目前這個 HttpContext 的 User 物件去
                //Context.User = new GenericPrincipal(Context.User.Identity, roles);

                if (Request.Url.Host == "127.0.0.1" || Request.Url.Host == "localhost" || Request.Url.Host == "::1")
                    return;

                DatabaseEntities entity = new DatabaseEntities();
                AuthLog log = new AuthLog();
                log.Path = Request.Path;
                log.UserHost = Request.UserHostAddress;
                log.AuthDate = DateTime.Now;
                log.UserName = User.Identity.Name;
                //UsersContext db = new UsersContext();
                //UserProfile user = db.UserProfiles.FirstOrDefault(u => u.UserName.ToLower() == User.Identity.Name.ToLower());
                //if (user != null)
                //{
                //    log.UserID = user.UserId;
                //}

                log.UserID = User.Identity.GetUserId();

                entity.AuthLog.Add(log);
                entity.SaveChanges();
            }
        }
    }
}
