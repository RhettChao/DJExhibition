using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Net.Mail;
using System.IO;

//using Reservation.Filters;
using Exhibition.Models;
using System.Web.Script.Serialization;

using Microsoft.AspNet.Identity;

namespace Exhibition.Controllers
{
    //[InitializeSimpleMembership]
    public class ReservationController : Controller
    {

        //StateCode:
        //10-送出申請 20-已認證 25-已核准 30-已取消
        static int StateCode_New = 10;
        static int StateCode_Verify = 20;
        //static int StateCode_Auth = 25;
        static int StateCode_Cancel = 30;

        static TimeSpan VerifyTimeSpan = new TimeSpan(24, 0, 0);

        //
        // GET: /ReservationTest/
        public ActionResult Index()
        {
            return Redirect("~/");
        }
        //public ActionResult ListTest()
        //{
        //    return View();
        //}
        public ActionResult ListNotAuth()
        {
            return View();
        }

        public ActionResult Health()
        {
            return View();
        }
        public ActionResult HealthForm()
        {
            return File(Server.MapPath("~/1091015_實名制-預約團體-實名制名冊.xlsx"), "application/octet-stream", "1091015_實名制-預約團體-實名制名冊.xlsx");
        }


        public ActionResult Members()
        {
            if (User.Identity.IsAuthenticated && User.IsInRole("AdminExhibition"))
                ViewBag.IsExhibitionAdmin = "true";
            else
                ViewBag.IsExhibitionAdmin = "false";
            return View();
        }
        public ActionResult DateAndTime(Reservation model)
        {
            //check identity!
            //if (model.TouristType == 2) {
            //    if (!User.Identity.IsAuthenticated || !User.IsInRole("AdminExhibition"))
            //        return RedirectToAction("Members");
            //}

            if (model.TouristType == null || model.TouristRange1 == null)
                return RedirectToAction("Index");
            else
            {
                ViewBag.TouristType = model.TouristType;
                ViewBag.TouristRange1 = model.TouristRange1;
                ViewBag.TouristRange2 = model.TouristRange2;
                ViewBag.ReservationBegin = model.ReservationBegin;
                return View();
            }
        }


        public ActionResult Date(Reservation model)
        {
            if (model.TouristType == null || model.TouristRange1 == null)
                return RedirectToAction("Index");
            else
            {
                ViewBag.TouristType = model.TouristType;
                ViewBag.TouristRange1 = model.TouristRange1;
                ViewBag.TouristRange2 = model.TouristRange2;
                ViewBag.ReservationBegin = model.ReservationBegin;
                return View();
            }
        }
        //public ActionResult Time(DateTime date)
        //{
        //    ViewBag.RequestDate = date.ToString("yyyy-MM-dd");
        //    return View();
        //}
        public ActionResult Time(Reservation model)
        {
            //ViewBag.RequestDate = model.ReservationBegin.Value.Date.ToString("yyyy-MM-dd");
            //for IE8 javascript datetime format!
            ViewBag.RequestDate = model.ReservationBegin.Value.Date.ToString("yyyy/MM/dd");

            ViewBag.TouristType = model.TouristType;
            ViewBag.TouristRange1 = model.TouristRange1;
            ViewBag.TouristRange2 = model.TouristRange2;
            ViewBag.ReservationBegin = model.ReservationBegin;
            ViewBag.ReservationEnd = model.ReservationEnd;
            return View();
        }
        public ActionResult Contact(Reservation model)
        {
            ViewBag.TouristType = model.TouristType;
            ViewBag.TouristRange1 = model.TouristRange1;
            ViewBag.TouristRange2 = model.TouristRange2;
            ViewBag.ReservationBegin = model.ReservationBegin;
            ViewBag.ReservationEnd = model.ReservationEnd;



            //ViewBag.ReservationDate = model.ReservationBegin.Value.ToString("yyyy-MM-dd") + " " + model.ReservationBegin.Value.ToString("HH:mm") + "~" + model.ReservationEnd.Value.ToString("HH:mm");


            //ViewBag.ReservationDate = model.ReservationBegin.Value.ToString("yyyy-MM-dd");
            //20160506 for CHT year
            int yCHT = model.ReservationBegin.Value.Year - 1911;
            ViewBag.ReservationDate = yCHT + model.ReservationBegin.Value.ToString("-MM-dd");
            DateTime noon = model.ReservationBegin.Value.Date.AddHours(12);
            if (model.ReservationBegin < noon)
                ViewBag.ReservationDate += " : 上午";
            else
                ViewBag.ReservationDate += " : 下午";

            if (model.TouristType == 1)
            {
                ViewBag.Tourist = "個人 : " + model.TouristRange1 + "人";
            }
            else if (model.TouristType == 2)
            {
                //ViewBag.Tourist = "團體 : " + model.TouristRange1 + "~" + model.TouristRange2 + "人";
                ViewBag.Tourist = "團體 : " + model.TouristRange1 + "人";
            }


            return View();
        }

        //public ActionResult Success(Models.ReservationContactModel model)
        public ActionResult Success(Reservation model)
        {
            DatabaseEntities entity = new DatabaseEntities();
            model.ID = Guid.NewGuid();
            model.CreateDate = DateTime.Now;
            model.CreateSource = Request.UserHostAddress;
            string userId = GetUserId();
            if (userId.Length > 0)
                model.CreateUserId = userId;
            string random = Guid.NewGuid().ToString().Replace("-", "");
            model.VerifyCode = random;
            model.StateCode = StateCode_New;
            //ViewBag.TouristRange = model.TouristRange1;
            //if (model.TouristType == 2)
            //{
            //    ViewBag.TouristRange += "~" + model.TouristRange2;
            //}
            //預約人數=實際人數
            //model.TouristNo = model.TouristRange1;
            entity.Reservation.Add(model);
            int rows = entity.SaveChanges();
            if (rows == 1)
            {
                string time = model.ReservationBegin.Value.Hour < 12 ? "上午" : "下午";
                ViewBag.Time = time;

                ViewBag.ReservationCode = model.SN.ToString("D5");
                string mailLog = "";
                try
                {
                    SmtpClient smtp = new SmtpClient(System.Configuration.ConfigurationManager.AppSettings["SMTPServer"]);
                    MailMessage msg = new MailMessage("淡江大橋願景館<no-reply@sinotech.com.tw>", model.ContactEmail);
                    //msg.Bcc.Add("rhett@apps.sinotech.com.tw");
                    //List<string> bccs = GetMailBCC();
                    //foreach (string bcc in bccs)
                    //    msg.Bcc.Add(bcc);
                    msg.Subject = "淡江大橋願景館 線上預約認證信";
                    msg.IsBodyHtml = false;
                    string strGroup = "";
                    if (model.TouristType == 2)
                    {
                        //strGroup = "團體名稱：" + model.GroupName + "(" + GetGroupTypeName(model.GroupType.Value) + ")";
                        strGroup = "團體名稱：" + model.GroupName;
                    }
                    //string str1 = "以上為您的預約資料，若確定無誤後請點選以下連結(或複製連結網址後於瀏覽器網址列貼上並前往)，即可正式完成預約流程。";
                    string str1 = "以上為您的預約資料，若確定無誤後請點選以下連結(或複製連結網址後於瀏覽器網址列貼上並前往)，以完成電子郵件認證程序。";
                    //string str2 = "http://" + Request.Url.Host + ":" + Request.Url.Port + VirtualPathUtility.ToAbsolute("~/Reservation/Verify") + "?v=" + random;
                    //20220318 認證信連結有誤 假設一律為https
                    //string httpMethod = "https";
                    //if (Request.Url.Port == 443)
                    //    httpMethod = "https";
                    string str2 = "https://" + Request.Url.Host + ":" + Request.Url.Port + VirtualPathUtility.ToAbsolute("~/Reservation/Verify") + "?v=" + random;
                    int yCHT = model.ReservationBegin.Value.Year - 1911;
                    string body = string.Format(@"                        
            預約代碼：{0}
            預約日期：{1}
            預約時段：{2}
            {3}
            聯絡人姓名：{4}
            行動電話：{5}
            電子郵件：{6}
            備註：{7}

{8}
{9}"
                        , model.SN.ToString("D5")
                        //, model.ReservationBegin.Value.ToString("HH:mm") + "~" + model.ReservationEnd.Value.ToString("HH:mm")
                        //, model.ReservationBegin.Value.Date.ToString("yyyy-MM-dd")
                        , yCHT + model.ReservationBegin.Value.Date.ToString("-MM-dd")
                        , time
                        , strGroup
                        , model.ContactName
                        , model.ContactMobile
                        , model.ContactEmail
                        , model.ContactNote, str1, str2);

                    msg.Body = body;
                    smtp.Send(msg);
                }
                catch (Exception ex)
                {
                    mailLog = ex.Message;
                }

                using (StreamWriter sw = new StreamWriter(Server.MapPath("~/MailSuccess.log"), true))
                {
                    if (mailLog == "")
                        sw.WriteLine("Success,Date:" + DateTime.Now.ToString() + ",SN:" + model.SN);
                    else
                        sw.WriteLine("Error  ,Date:" + DateTime.Now.ToString() + ",SN:" + model.SN + ",Error Message:" + mailLog);
                    sw.Close();
                }

                //ViewBag.ReservationCode = model.SN.ToString("D5");
                ViewBag.Msg = "預約資訊";
                return View(model);
            }
            else
            {
                ViewBag.Msg = "預約失敗！";
                return View();
            }

            //if (model.ContactName == null ||
            //    model.ContactMobile == null ||
            //    model.ContactEmail == null ||
            //    model.ReservationBegin == null ||
            //    model.ReservationEnd == null)
            //{
            //    ViewBag.Msg = "預約失敗！";
            //    return View();
            //}
            //else
            //{

            //}
            //ViewBag.ContactName = model.ContactName;
            //ViewBag.ContactName = Request["ContactName"];
            //ViewBag.ContactName = Request.Form["ContactName"];            
        }

        public ActionResult Verify(string v)
        {
            DatabaseEntities entity = new DatabaseEntities();
            var rows = (from row in entity.Reservation
                        where row.VerifyCode == v
                        orderby row.SN descending
                        select row).FirstOrDefault();

            if (rows == null)
            {
                return View("VerifyNull");
            }
            else
            {
                if (rows.VerifyDate != null)
                {
                    return View("VerifyRedundant");
                }
                else
                {
                    if (rows.CreateDate == null)
                        return View("VerifyNull");

                    TimeSpan span = DateTime.Now - rows.CreateDate.Value;
                    if (span > VerifyTimeSpan || rows.StateCode == StateCode_Cancel)
                    {
                        return View("VerifyExpired");
                    }

                    ViewBag.ReservationCode = rows.SN.ToString("D5");
                    ViewBag.TouristRange = rows.TouristRange1;
                    if (rows.TouristType == 2)
                        ViewBag.TouristRange += "~" + rows.TouristRange2;
                    rows.VerifyDate = DateTime.Now;
                    rows.VerifySource = Request.UserHostAddress;
                    string userId = GetUserId();
                    if (userId.Length > 0)
                        rows.VerifyUserId = userId;
                    rows.StateCode = StateCode_Verify;
                    entity.SaveChanges();
                    string mailLog = "";
                    try
                    {
                        SmtpClient smtp = new SmtpClient(System.Configuration.ConfigurationManager.AppSettings["SMTPServer"]);
                        MailMessage msg = new MailMessage("淡江大橋願景館<no-reply@sinotech.com.tw>", rows.ContactEmail);
                        //msg.Bcc.Add("rhett@apps.sinotech.com.tw");
                        List<string> bccs = GetMailBCC();
                        foreach (string bcc in bccs)
                            msg.Bcc.Add(bcc);
                        //msg.Subject = "淡江大橋願景館 線上預約完成通知";
                        msg.Subject = "淡江大橋願景館 電子郵件認證成功";
                        msg.IsBodyHtml = false;
                        string strGroup = "";
                        if (rows.TouristType == 2)
                        {
                            //strGroup = "團體名稱：" + rows.GroupName + "(" + GetGroupTypeName(rows.GroupType.Value) + ")";
                            strGroup = "團體名稱：" + rows.GroupName;
                        }
                        int yCHT = rows.ReservationBegin.Value.Year - 1911;
                        string body = string.Format(@"                        
            預約代碼：{0}
            預約日期：{1}
            預約時段：{2}
            {3}            
            聯絡人姓名：{4}
            行動電話：{5}
            電子郵件：{6}
            備註：{7}

"
                            , rows.SN.ToString("D5")
                            //, rows.ReservationBegin.Value.Date.ToString("yyyy-MM-dd")
                            , yCHT + rows.ReservationBegin.Value.Date.ToString("-MM-dd")
                            //, rows.ReservationBegin.Value.ToString("HH:mm") + "~" + rows.ReservationEnd.Value.ToString("HH:mm")
                            , rows.ReservationBegin.Value.Hour < 12 ? "上午" : "下午"
                            , strGroup
                            , rows.ContactName
                            , rows.ContactMobile
                            , rows.ContactEmail
                            , rows.ContactNote);
                        //string note = "線上預約已成功！";
                        string note = "電子郵件認證成功，進入審核程序，待管理者確認後將另行通知！";
                        msg.Body = body + note;
                        smtp.Send(msg);
                    }
                    catch (Exception ex)
                    {
                        mailLog = ex.Message;
                    }
                    using (StreamWriter sw = new StreamWriter(Server.MapPath("~/MailVerify.log"), true))
                    {
                        if (mailLog == "")
                            sw.WriteLine("Success,Date:" + DateTime.Now.ToString() + ",SN:" + rows.SN);
                        else
                            sw.WriteLine("Error  ,Date:" + DateTime.Now.ToString() + ",SN:" + rows.SN + ",Error Message:" + mailLog);
                        sw.Close();
                    }

                    return View(rows);
                }
            }
        }

        [Authorize()]
        public ActionResult Guest()
        {
            ViewBag.User = User.Identity.Name;
            if (!User.IsInRole("AdminExhibition"))
            {
                return View("ListNotAuth");
            }
            else
            {
                return View();
            }
        }
        [Authorize()]
        public ActionResult GuestData(DateTime? dateBegin, DateTime? dateEnd)
        {
            DatabaseEntities entity = new DatabaseEntities();

            //auto cancel !
            DateTime expireDate = DateTime.Now - VerifyTimeSpan;
            var expireRows = from row in entity.Reservation
                             where row.StateCode == StateCode_New && row.CreateDate < expireDate
                             select row;
            foreach (var row in expireRows)
            {
                row.StateCode = StateCode_Cancel;
                row.CancelDate = DateTime.Now;
                row.CancelUserId = "-1";
            }
            entity.SaveChanges();




            if (dateBegin == null)
                dateBegin = new DateTime(2000, 1, 1);
            if (dateEnd == null)
                dateEnd = new DateTime(2100, 1, 1);

            dateBegin = dateBegin.Value.Date;
            dateEnd = dateEnd.Value.Date.AddDays(1);


            var rows = from row in entity.Reservation
                       where row.StateCode != StateCode_Cancel && row.ReservationBegin >= dateBegin && row.ReservationBegin < dateEnd
                       group row by 1 into g
                       select new
                       {
                           SumTouristRange1 = g.Sum(x => x.TouristRange1),
                           SumTouristNo = g.Sum(x => x.TouristNo),
                           SumNationForeigner = g.Sum(x => x.NationForeigner),
                           SumNationNative = g.Sum(x => x.NationNative),
                           SumMemberGov = g.Sum(x => x.MemberGov),
                           SumMemberNGO = g.Sum(x => x.MemberNGO),
                           SumMemberEdu = g.Sum(x => x.MemberEdu),
                           SumMemberOther = g.Sum(x => x.MemberOther)
                       };
            return Json(rows, JsonRequestBehavior.AllowGet);
        }


        [Authorize()]
        //[Authorize(Roles = "AdminExhibition")]
        //[InitializeSimpleMembership]
        public ActionResult List()
        {
            ViewBag.User = User.Identity.Name;
            //if (!Roles.IsUserInRole("AdminExhibition"))
            if (!User.IsInRole("AdminExhibition"))
            //if(!User.Identity.IsAuthenticated)
            {
                //return "<h2>此帳號尚未通過管理者認證！</h2>";
                return View("ListNotAuth");
            }
            else
            {
                return View();
            }
        }



        public ActionResult ListAll()
        {
            DatabaseEntities entity = new DatabaseEntities();

            //auto cancel !
            DateTime expireDate = DateTime.Now - VerifyTimeSpan;
            var expireRows = from row in entity.Reservation
                             where row.StateCode == StateCode_New && row.CreateDate < expireDate
                             select row;
            foreach (var row in expireRows)
            {
                row.StateCode = StateCode_Cancel;
                row.CancelDate = DateTime.Now;
                row.CancelUserId = "-1";
            }
            entity.SaveChanges();


            //var rows = (from row in entity.Reservation
            //            orderby row.SN descending                        
            //            select new
            //            {
            //                row.SN,
            //                row.ID,
            //                row.ReservationBegin,
            //                row.ReservationEnd,
            //                ReservationCode = row.SN,
            //                row.ContactName,
            //                row.ContactMobile,
            //                row.ContactEmail,
            //                row.ContactNote,
            //                row.CreateDate,
            //                row.GroupName,
            //                row.TouristType,
            //                row.TouristRange1,
            //                row.TouristRange2,
            //                row.VerifyDate
            //            }).ToList();

            var rows = from row in entity.Reservation
                       join s1 in entity.SystemCode
                       on new
                       {
                           Type = "StateCode",
                           ID = row.StateCode
                       } equals
                       new
                       {
                           s1.Type,
                           ID = (int?)s1.ID
                       }
                           //row.StateCode equals s1.ID 
                       into g1
                       from group1 in g1.DefaultIfEmpty()
                       //orderby row.SN descending
                       select new
                       {
                           row.SN,
                           //row.ID,
                           row.ReservationBegin,
                           //row.ReservationEnd,
                           //ReservationCode = row.SN,
                           row.GroupName,
                           row.ContactName,
                           //row.ContactMobile,
                           //row.ContactEmail,
                           //row.ContactNote,
                           row.CreateDate,
                           //row.GroupName,
                           row.TouristType,
                           row.TouristRange1,
                           //row.TouristRange2,
                           row.TouristNo,
                           //row.VerifyDate,
                           StateCodeName = group1.Name,
                           row.AuthDate
                       };

            return Json(rows, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ListAllOne(int sn)
        {
            DatabaseEntities entity = new DatabaseEntities();

            var rows = from row in entity.Reservation
                       join s1 in entity.SystemCode
                       on new
                       {
                           Type = "StateCode",
                           ID = row.StateCode
                       } equals
                       new
                       {
                           s1.Type,
                           ID = (int?)s1.ID
                       }
                           //row.StateCode equals s1.ID 
                       into g1
                       from group1 in g1.DefaultIfEmpty()
                       where row.SN == sn
                       //orderby row.SN descending
                       select new
                       {
                           row.SN,
                           //row.ID,
                           row.ReservationBegin,
                           //row.ReservationEnd,
                           //ReservationCode = row.SN,
                           row.GroupName,
                           row.ContactName,
                           //row.ContactMobile,
                           //row.ContactEmail,
                           //row.ContactNote,
                           row.CreateDate,
                           //row.GroupName,
                           row.TouristType,
                           row.TouristRange1,
                           //row.TouristRange2,
                           row.TouristNo,
                           //row.VerifyDate,
                           StateCodeName = group1.Name,
                           row.AuthDate
                       };

            return Json(rows, JsonRequestBehavior.AllowGet);
        }


        public ActionResult ListSingle(int sn)
        {
            DatabaseEntities entity = new DatabaseEntities();
            var rows = (from row in entity.Reservation
                        //join sys in entity.SystemCode
                        //on row.GroupType equals sys.ID into resGroup
                        //from item in resGroup.DefaultIfEmpty()

                        join sys in entity.SystemCode
                          on new
                          {
                              Type = "GroupType",
                              ID = row.GroupType
                          } equals
                          new
                          {
                              sys.Type,
                              ID = (int?)sys.ID
                          }
                          into resGroup
                        from item in resGroup.DefaultIfEmpty()

                        join s1 in entity.SystemCode
                          on new
                          {
                              Type = "StateCode",
                              ID = row.StateCode
                          } equals
                          new
                          {
                              s1.Type,
                              ID = (int?)s1.ID
                          }
                          into g1
                        from group1 in g1.DefaultIfEmpty()

                        where row.SN == sn
                        select new
                        {
                            row.SN,
                            row.ID,
                            row.ReservationBegin,
                            row.ReservationEnd,
                            row.ContactName,
                            row.ContactMobile,
                            row.ContactEmail,
                            row.ContactNote,
                            row.CreateDate,
                            row.TouristType,
                            row.TouristRange1,
                            row.GroupName,
                            row.GroupType,
                            row.TouristNo,
                            row.VerifyDate,
                            row.ModifyDate,
                            row.Note,
                            row.NationForeigner,
                            row.NationNative,
                            row.MemberGov,
                            row.MemberNGO,
                            row.MemberEdu,
                            row.MemberOther,
                            GroupTypeName = item.Name,
                            StateCodeName = group1.Name,
                            row.StateCode,
                            row.TourNo,
                            row.TourLeader,
                            row.TourGuide,
                            row.TourField,
                            row.AuthDate
                        }).ToList();

            return Json(rows, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ListSingle_1(int sn)
        {
            DatabaseEntities entity = new DatabaseEntities();
            var rows = (from row in entity.Reservation
                        join sys in entity.SystemCode
                        on row.GroupType equals sys.ID into resGroup
                        from item in resGroup.DefaultIfEmpty()
                        where row.SN == sn
                        select new
                        {
                            row.SN,
                            row.ID,
                            row.ReservationBegin,
                            row.ReservationEnd,
                            row.ContactName,
                            row.ContactMobile,
                            row.ContactEmail,
                            row.ContactNote,
                            row.CreateDate,
                            row.TouristType,
                            row.TouristRange1,
                            row.GroupName,
                            row.GroupType,
                            row.TouristNo,
                            row.VerifyDate,
                            row.ModifyDate,
                            row.Note,
                            row.NationForeigner,
                            row.NationNative,
                            row.MemberGov,
                            row.MemberNGO,
                            row.MemberEdu,
                            row.MemberOther,
                            GroupTypeName = item.Name
                        }).ToList();

            return Json(rows, JsonRequestBehavior.AllowGet);
        }

        //public ActionResult ListAllJS()
        //{
        //    List<List<string>> lists = new List<List<string>>();

        //    DatabaseEntities entity = new DatabaseEntities();
        //    var rows = from row in entity.Reservation
        //               orderby row.SN descending
        //               select row;
        //    foreach (var row in rows)
        //    {
        //        List<string> list = new List<string>();
        //        list.Add(row.SN.ToString());
        //        list.Add(row.ReservationBegin.Value.Date.ToString("yyyy-MM-dd"));
        //    }

        //    return Json(rows, JsonRequestBehavior.AllowGet);
        //}

        public ActionResult Query()
        {
            return View();
        }
        public ActionResult QueryJS(string code, string email)
        {
            int sn = 0;
            int.TryParse(code, out sn);

            DatabaseEntities entity = new DatabaseEntities();
            var rows = (from row in entity.Reservation
                        where row.SN == sn && row.ContactEmail == email
                        orderby row.SN descending
                        select row).FirstOrDefault();
            if (rows != null && rows.StateCode != StateCode_Verify)
            {
                int c = rows.StateCode.Value;
                rows = new Reservation();
                rows.StateCode = c;
            }

            return Json(rows, JsonRequestBehavior.AllowGet);
        }



        [Authorize]
        public ActionResult Offday()
        {
            if (!User.IsInRole("AdminExhibition"))
                return RedirectToAction("Index");
            return View();
        }
        [Authorize]
        public ActionResult OffdayList()
        {
            DatabaseEntities entity = new DatabaseEntities();
            var rows = from row in entity.Holiday
                       orderby row.OffDate descending
                       select row;

            return Json(rows, JsonRequestBehavior.AllowGet);
        }
        [Authorize]
        public ActionResult OffdayAdd(DateTime offDate, string title)
        {
            DatabaseEntities entity = new DatabaseEntities();
            Holiday h = new Holiday();
            h.OffDate = offDate.Date;
            title = title.Trim();
            if (title.Length > 0)
                h.Title = title;
            h.CreateDate = DateTime.Now;
            h.CreateSource = Request.UserHostAddress;
            h.CreateUserName = User.Identity.Name;
            var row = entity.Holiday.Add(h);
            try
            {
                int rows = entity.SaveChanges();
                return Json(row, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                List<string> list = new List<string>();
                return Json(list, JsonRequestBehavior.AllowGet);
            }
        }
        [Authorize]
        public int OffdayDelete(DateTime offDate)
        {
            bool removed = false;
            offDate = offDate.Date;
            DatabaseEntities entity = new DatabaseEntities();
            try
            {
                var rows = (from row in entity.Holiday
                            where row.OffDate == offDate
                            select row).FirstOrDefault();
                if (rows != null)
                {
                    entity.Holiday.Remove(rows);
                    if (entity.SaveChanges() == 1)
                        removed = true;
                }
            }
            catch
            {

            }
            return Convert.ToInt32(removed);
        }
        [Authorize]
        //public ActionResult UpdateReservation(Guid id, int? touristNo, string note, int? NationForeigner, int? NationNative, int? MemberGov, int? MemberNGO, int? MemberEdu, int? MemberOther, string TourNo, string TourLeader, string TourGuide, string TourField)
        public ActionResult UpdateReservation(Guid id, int? touristNo, string note, int? NationForeigner, int? NationNative, int? MemberGov, int? MemberNGO, int? MemberEdu, int? MemberOther, string TourNo, string TourLeader, string TourGuide, string TourField, string GroupName, int? TouristRange1, string ContactName, string ContactMobile, string ContactEmail, string ContactNote)
        {
            int rows = 0;
            DatabaseEntities entity = new DatabaseEntities();

            var res = (from row in entity.Reservation
                       where row.ID == id
                       select row).FirstOrDefault();
            if (res != null)
            {
                //if (touristNo != null)
                //    res.TouristNo = touristNo;
                //else

                res.TouristNo = touristNo;
                res.Note = note.Trim();
                res.ModifyDate = DateTime.Now;
                res.ModifySource = Request.UserHostAddress;
                res.NationForeigner = NationForeigner;
                res.NationNative = NationNative;
                res.MemberEdu = MemberEdu;
                res.MemberGov = MemberGov;
                res.MemberNGO = MemberNGO;
                res.MemberOther = MemberOther;
                res.TourNo = TourNo;
                res.TourLeader = TourLeader;
                res.TourGuide = TourGuide;
                res.TourField = TourField;
                string userId = GetUserId();
                if (userId.Length > 0)
                    res.ModifyUserId = userId;

                res.GroupName = GroupName;
                res.TouristRange1 = TouristRange1;
                res.TouristRange2 = TouristRange1;
                res.ContactName = ContactName;
                res.ContactMobile = ContactMobile;
                res.ContactEmail = ContactEmail;
                res.ContactNote = ContactNote;

                rows = entity.SaveChanges();
            }

            return Json(rows, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CancelReservation(Guid id)
        {
            int rows = 0;
            DatabaseEntities entity = new DatabaseEntities();

            var res = (from row in entity.Reservation
                       where row.ID == id
                       select row).FirstOrDefault();
            if (res != null)
            {
                res.CancelDate = DateTime.Now;
                res.CancelSource = Request.UserHostAddress;
                //res.StateCode = "CANCEL";
                res.StateCode = StateCode_Cancel;
                if (User.Identity.IsAuthenticated)
                {
                    //紀錄取消者身分                    
                    //UsersContext db = new UsersContext();
                    //UserProfile user = db.UserProfiles.FirstOrDefault(u => u.UserName.ToLower() == User.Identity.Name.ToLower());
                    //if (user != null)
                    //{
                    //    res.CancelUserId = user.UserId;
                    //}
                    res.CancelUserId = GetUserId();
                }                
                rows = entity.SaveChanges();
            }

            return Json(rows, JsonRequestBehavior.AllowGet);
        }
        [Authorize]
        public ActionResult VerifyReservation(Guid id)
        {
            int rows = 0;
            DatabaseEntities entity = new DatabaseEntities();

            var res = (from row in entity.Reservation
                       where row.ID == id
                       select row).FirstOrDefault();
            if (res != null)
            {
                res.VerifyDate = DateTime.Now;
                res.VerifySource = Request.UserHostAddress;
                string userId = GetUserId();
                if (userId.Length > 0)
                    res.VerifyUserId = userId;
                res.StateCode = StateCode_Verify;
                rows = entity.SaveChanges();
            }

            return Json(rows, JsonRequestBehavior.AllowGet);
        }

        public List<string> GetMailBCC() {
            List<string> bcc = new List<string>();            

            DatabaseEntities entity = new DatabaseEntities();
            //var rows = from row in entity.UserProfileExt
            //           where row.ReceiveCopy == true
            //           select row;
            var rows = entity.AspNetUsers_Ext.Where(u => u.ReceiveCopy == true && u.UserId != null);
            
            foreach (var row in rows)
                bcc.Add(row.RealEmail);
            return bcc;
        }


        [Authorize]
        public ActionResult AuthReservation(Guid id)
        {
            int results = 0;
            DatabaseEntities entity = new DatabaseEntities();

            var rows = (from row in entity.Reservation
                        where row.ID == id
                        select row).FirstOrDefault();
            if (rows != null)
            {
                rows.AuthDate = DateTime.Now;
                rows.AuthSource = Request.UserHostAddress;
                string userId = GetUserId();
                if (userId.Length > 0)
                    rows.AuthUserId = userId;
                //results = entity.SaveChanges();

                //send mail
                string mailLog = "";
                try
                {
                    SmtpClient smtp = new SmtpClient(System.Configuration.ConfigurationManager.AppSettings["SMTPServer"]);
                    MailMessage msg = new MailMessage("淡江大橋願景館<no-reply@sinotech.com.tw>", rows.ContactEmail);
                    //msg.Bcc.Add("rhett@apps.sinotech.com.tw");
                    List<string> bccs = GetMailBCC();
                    foreach (string bcc in bccs)
                        msg.Bcc.Add(bcc);
                    msg.Subject = "淡江大橋願景館 線上預約完成通知";
                    msg.IsBodyHtml = false;
                    string strGroup = "";
                    if (rows.TouristType == 2)
                    {
                        //strGroup = "團體名稱：" + rows.GroupName + "(" + GetGroupTypeName(rows.GroupType.Value) + ")";
                        strGroup = "團體名稱：" + rows.GroupName;
                    }
                    int yCHT = rows.ReservationBegin.Value.Year - 1911;
                    string body = string.Format(@"                        
            預約代碼：{0}
            預約日期：{1}
            預約時段：{2}
            {3}            
            聯絡人姓名：{4}
            行動電話：{5}
            電子郵件：{6}
            備註：{7}

線上預約已通過審核！

《為配合中央流行疫情指揮中心，本館將採實名制登記，煩請至以下網址詳閱防疫相關規定，並填妥參訪團體名冊表格於參訪日前三日回傳，以縮短您入館的程序。非常感謝您的配合！》
※本館防疫相關規定：http://djbridge.sinotech.com.tw/Exhibition/Reservation/Health

（本郵件為系統自動發送，請勿直接回覆！）

"
                        , rows.SN.ToString("D5")
                        //, rows.ReservationBegin.Value.Date.ToString("yyyy-MM-dd")
                        , yCHT + rows.ReservationBegin.Value.Date.ToString("-MM-dd")
                        //, rows.ReservationBegin.Value.ToString("HH:mm") + "~" + rows.ReservationEnd.Value.ToString("HH:mm")
                        , rows.ReservationBegin.Value.Hour < 12 ? "上午" : "下午"
                        , strGroup
                        , rows.ContactName
                        , rows.ContactMobile
                        , rows.ContactEmail
                        , rows.ContactNote);
                    //string note = "線上預約已通過審核！";
                    string note = "";
                    msg.Body = body + note;
                    smtp.Send(msg);

                    results = entity.SaveChanges();
                }
                catch (Exception ex)
                {
                    mailLog = ex.Message;
                }
                using (StreamWriter sw = new StreamWriter(Server.MapPath("~/MailVerify.log"), true))
                {
                    if (mailLog == "")
                        sw.WriteLine("Success,Date:" + DateTime.Now.ToString() + ",SN:" + rows.SN);
                    else
                        sw.WriteLine("Error  ,Date:" + DateTime.Now.ToString() + ",SN:" + rows.SN + ",Error Message:" + mailLog);
                    sw.Close();
                }

            }
            return Json(results, JsonRequestBehavior.AllowGet);
        }


        public ActionResult ChangeReservationDate(Guid id, DateTime begin, DateTime end)
        {
            int rows = 0;
            DatabaseEntities entity = new DatabaseEntities();

            var res = (from row in entity.Reservation
                       where row.ID == id
                       select row).FirstOrDefault();
            if (res != null)
            {
                res.ReservationBegin = begin;
                res.ReservationEnd = end;

                res.ModifyDate = DateTime.Now;
                res.ModifySource = Request.UserHostAddress;
                string userId = GetUserId();
                if (userId.Length > 0)
                    res.ModifyUserId = userId;

                rows = entity.SaveChanges();
            }

            return Json(rows, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetTouristInfo() {
            DateTime today = DateTime.Now.Date;
            DatabaseEntities entity = new DatabaseEntities();
            var groups = (from row in entity.Reservation
                          where row.StateCode == StateCode_Verify && row.ReservationBegin.Value < today
                          select row).Count();
            var tourists = (from row in entity.Reservation
                            where row.StateCode == StateCode_Verify && row.ReservationBegin.Value < today
                            select row.TouristNo).Sum().GetValueOrDefault();
            Dictionary<string, int> result = new Dictionary<string, int>();
            result.Add("groups", groups);
            result.Add("tourists", tourists);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetTouristInfoP()
        {
            //Response.ContentType = "application/x-javascript";

            string callback = "callback";
            if (Request["callback"] != null)
                callback = Request["callback"];
            DateTime today = DateTime.Now.Date;
            DatabaseEntities entity = new DatabaseEntities();
            var groups = (from row in entity.Reservation
                          where row.StateCode == StateCode_Verify && row.ReservationBegin.Value < today
                          select row).Count();
            var tourists = (from row in entity.Reservation
                            where row.StateCode == StateCode_Verify && row.ReservationBegin.Value < today
                            select row.TouristNo).Sum().GetValueOrDefault();
            Dictionary<string, int> result = new Dictionary<string, int>();
            result.Add("groups", groups);
            result.Add("tourists", tourists);

            List<object> list = new List<object>();
            list.Add(result);

            JavaScriptSerializer ser = new JavaScriptSerializer();
            string p = callback + "(" + ser.Serialize(list) + ");";
            
            //return Json(result, JsonRequestBehavior.AllowGet);
            return Content(p, "application/x-javascript");

        }


        public string _GetGroupTypeName(int id)
        {
            DatabaseEntities entity = new DatabaseEntities();
            var rows = (from row in entity.SystemCode
                        where row.Type == "GroupType" && row.ID == id
                        select row.Name).FirstOrDefault();
            return rows;

            //string name = "";
            //switch (id) { 
            //    case 1:
            //        name = "政府機關";
            //        break;
            //    case 2:
            //        name = "民間團體";
            //        break;
            //    case 3:
            //        name = "學校";
            //        break;
            //    case 99:
            //        name = "其他";
            //        break;
            //}
            //return name;
        }

        //int GetUserId()
        string GetUserId()
        {
            //int id = 0;
            string id = "";
            if (User.Identity.IsAuthenticated)
            {
                //UsersContext db = new UsersContext();
                //UserProfile user = db.UserProfiles.FirstOrDefault(u => u.UserName.ToLower() == User.Identity.Name.ToLower());
                //if (user != null)
                //{
                //    id = user.UserId;
                //}
                id = User.Identity.GetUserId();
            }
            return id;
        }
    }
}

