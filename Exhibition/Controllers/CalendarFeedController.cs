using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Exhibition.Models;

namespace Exhibition.Controllers
{
    public class CalendarFeedController : Controller
    {
        //static int reservationMinDays = 7;
        static int reservationMinDays = 10;
        static int reservationMaxDays = 40;
        static int dailyBeginHour = 8;
        static int dailyEndHour = 17;
        //static int dailyBreakHour = 12;
        //每日可參觀人數總額=時段數X時段人數上限(個人+團體)
        static int daySlots = dailyEndHour - dailyBeginHour - 1;
        static int slotMaxTourists = 30;
        static int dayMaxTourists = daySlots * slotMaxTourists;


        static string timeAM_1 = "09:30";
        static string timeAM_2 = "10:30";
        static string timePM_1 = "14:00";
        static string timePM_2 = "15:00";

        static TimeSpan VerifyTimeSpan = new TimeSpan(24, 0, 0);
        //
        // GET: /CalendarFeed/
        /*
        public ActionResult IndividualOpen(int touristNo)
        {
            DateTime today = DateTime.Now.Date;
            DateTime endDate = today.AddDays(reservationMaxDays);

            DatabaseEntities entity = new DatabaseEntities();
            var rows = from row in entity.Reservation
                       where row.ReservationBegin >= today && row.ReservationBegin <= endDate
                       select row;

            JsonResult jr = new JsonResult();

            List<FullCalendarEventObj> idvs = new List<FullCalendarEventObj>();
            
            int id = 0;
            for (int i = 0; i < reservationMaxDays; i++)
            {
                DateTime date = today.AddDays(i);
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                    continue;
                DateTime dateNext = date.AddDays(1);
                var rowDay = (from row in rows
                              where row.ReservationBegin >= date && row.ReservationBegin < dateNext
                              select row.TouristRange2).Sum();
                int max = dayMaxTourists - touristNo;
                if (rowDay != null)
                    max = max - rowDay.Value;
                FullCalendarEventObj a = new FullCalendarEventObj();
                if (max > 0)
                    a.title = "個人：可預約(" + max + ")";
                else
                    a.title = "個人：已額滿";                
                a.id = ++id;                
                a.allDay = true;                
                a.start = date.ToString(@"yyyy-MM-dd\THH:mm:ss\Z");                
                idvs.Add(a);                
            }
            return Json(idvs, JsonRequestBehavior.AllowGet);
        }
        */
        public ActionResult OpenInfo(int touristType, int touristNo) {
            //if (touristType == 1)
            //{
            //    return IdvInfo(touristNo);
            //}
            //else if (touristType == 2)
            //{
            //    return GroupInfo();
            //}
            //else
            //{
            //    JsonResult jr = new JsonResult();
            //    return Json(jr, JsonRequestBehavior.AllowGet);

            DateTime now = DateTime.Now;
            DateTime today = now.Date;
            DateTime beginDate = today.AddDays(reservationMinDays);
            //DateTime endDate = today.AddDays(reservationMaxDays);
            //20220318 bug:最後一天已預約資料無法顯示，會造成重複預約!
            DateTime endDate = today.AddDays(reservationMaxDays + 1);
            DateTime expiredDate = now - VerifyTimeSpan;

            DatabaseEntities entity = new DatabaseEntities();

            var rowsGroup = (from row in entity.Reservation
                             where row.TouristType == 2 && row.ReservationBegin >= today && row.ReservationBegin <= endDate
                             //&& row.StateCode < 30
                             && ((row.StateCode == 10 && row.CreateDate > expiredDate) || row.StateCode == 20)
                             
                             select row).ToList();
            //var rowsGroupAM = (from row in entity.Reservation
            //                   where row.TouristType == 2 && row.ReservationBegin >= today && row.ReservationBegin <= endDate && row.ReservationBegin.Value.Hour < 12
            //                   select new
            //                   {
            //                       row.ReservationBegin.Value
            //                   }).ToDictionary(d => d.Value, d => d.Value);
            //var rowsGroupPM = (from row in entity.Reservation
            //                   where row.TouristType == 2 && row.ReservationBegin >= today && row.ReservationBegin <= endDate && row.ReservationBegin.Value.Hour >= 12
            //                   select new { 
            //                       row.ReservationBegin.Value
            //                   }).ToDictionary(d => d.Value, d => d.Value);


            List<Reservation> rowsIdv = new List<Reservation>();
            if (touristType == 1)
                rowsIdv = (from row in entity.Reservation
                           where row.TouristType == 1 && row.ReservationBegin >= today && row.ReservationBegin <= endDate
                           //&& row.StateCode < 30
                           && ((row.StateCode == 10 && row.CreateDate > expiredDate) || row.StateCode == 20)
                           select row).ToList();
            

            //var rows = (from row in entity.Reservation
            //               where row.TouristType == 1 && row.ReservationBegin >= today && row.ReservationBegin <= endDate
            //               select row).ToList();            

            //var holidays = (from row in entity.Holiday
            //                where row.OffDate >= today
            //                select row).ToList();

            var holidays = (from row in entity.Holiday
                            where row.OffDate >= today
                            select row).ToDictionary(d => d.OffDate, d => d.Title);

            JsonResult jr = new JsonResult();

            List<FullCalendarEventObj> idvs = new List<FullCalendarEventObj>();

            int id = 0;
            DateTime date = beginDate;
            //for (int i = 0; i < reservationMaxDays; i++)
            //20220318 bug:最後一天已預約資料無法顯示，會造成重複預約!
            while (date < endDate)
            {
                //DateTime date = today.AddDays(i);
                //例假休館日
                //var offDays = (from row in holidays
                //               //where row.OffDate == date
                //               where row.Key == date
                //               select row).Any();
                var offDay = (from row in holidays
                              where row.Key == date
                              select row).FirstOrDefault();


                //if (offDays)
                if (offDay.Key != DateTime.MinValue)
                {

                    FullCalendarEventObj a = new FullCalendarEventObj();
                    //string t = offDay.Value == null ? "" : "(" + offDay.Value + ")";
                    string t = offDay.Value + "";
                    if (t.Length > 0)
                        t = "(" + t + ")";
                    a.title = "休館日" + t;
                    a.className = "offday";
                    a.id = ++id;
                    a.allDay = true;
                    a.start = date.ToString(@"yyyy-MM-dd\THH:mm:ss\Z");
                    idvs.Add(a);
                }
                //周一休館
                else if (date.DayOfWeek == DayOfWeek.Monday)
                {
                    FullCalendarEventObj a = new FullCalendarEventObj();
                    string t = offDay.Value + "";
                    if (t.Length > 0)
                        t = "(" + t + ")";
                    a.title = "休館日" + t;
                    a.className = "offday";
                    a.id = ++id;
                    a.allDay = true;
                    a.start = date.ToString(@"yyyy-MM-dd\THH:mm:ss\Z");
                    idvs.Add(a);
                }
                else if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                {
                    FullCalendarEventObj a = new FullCalendarEventObj();
                    a.title = "例假日";
                    a.className = "holiday";
                    a.id = ++id;
                    a.allDay = true;
                    a.start = date.ToString(@"yyyy-MM-dd\THH:mm:ss\Z");
                    idvs.Add(a);
                }
                else
                {

                    DateTime dateNext = date.AddHours(12);
                    var groupAMAny = (from row in rowsGroup
                                      where row.ReservationBegin >= date && row.ReservationBegin < dateNext
                                      select row).Any();
                    DateTime dateTommorrow = date.AddDays(1);
                    var groupPMAny = (from row in rowsGroup
                                      where row.ReservationBegin >= dateNext && row.ReservationBegin < dateTommorrow
                                      select row).Any();

                    //var groupAMAny = (from row in rowsGroupAM
                    //                  where row.Key == date
                    //                  select row).Any();
                    //DateTime dateTommorrow = date.AddDays(1);
                    //var groupPMAny = (from row in rowsGroupPM
                    //                  where row.Key == date
                    //                  select row).Any();

                    //個人                    
                    if (touristType == 1)
                    {
                        if (date.DayOfWeek != DayOfWeek.Friday)
                        {
                            FullCalendarEventObj a = new FullCalendarEventObj();
                            a.title = "團體參觀";
                            a.className = "grouponly";
                            a.id = ++id;
                            a.allDay = true;
                            a.start = date.ToString(@"yyyy-MM-dd\THH:mm:ss\Z");
                            idvs.Add(a);
                        }
                        else
                        {
                            //上午
                            FullCalendarEventObj a = new FullCalendarEventObj();
                            bool openAM = true;
                            int openAMNo = 0;
                            if (groupAMAny)
                            {
                                openAM = false;
                            }
                            else
                            {
                                var idvNo = (from row in rowsIdv
                                             where row.ReservationBegin >= date && row.ReservationBegin < dateNext
                                             select row.TouristRange1).Sum().Value;
                                openAMNo = slotMaxTourists - idvNo;
                                if ((touristNo + idvNo) > slotMaxTourists)
                                    openAM = false;
                            }
                            if (openAM)
                            {
                                a.title = "上午：可預約 " + openAMNo;
                                a.className = "am ok";
                            }
                            else
                            {
                                a.title = "上午：已額滿";
                                a.className = "am full";
                            }
                            a.id = ++id;
                            a.start = date.ToString(@"yyyy-MM-dd\T" + timeAM_1 + @":ss\Z");
                            a.end = date.ToString(@"yyyy-MM-dd\T" + timeAM_2 + @":ss\Z");
                            a.allDay = false;


                            idvs.Add(a);

                            //下午                            
                            FullCalendarEventObj p = new FullCalendarEventObj();
                            bool openPM = true;
                            int openPMNo = 0;
                            if (groupPMAny)
                            {
                                openPM = false;
                            }
                            else
                            {
                                var idvNo = (from row in rowsIdv
                                             where row.ReservationBegin >= dateNext && row.ReservationBegin < dateTommorrow
                                             select row.TouristRange1).Sum().Value;
                                openPMNo = slotMaxTourists - idvNo;
                                if ((touristNo + idvNo) > slotMaxTourists)
                                    openPM = false;
                            }
                            if (openPM)
                            {
                                p.title = "下午：可預約 " + openPMNo;
                                p.className = "pm ok";
                            }
                            else
                            {
                                p.title = "下午：已額滿";
                                p.className = "pm full";
                            }
                            p.start = date.ToString(@"yyyy-MM-dd\T" + timePM_1 + @":ss\Z");
                            p.end = date.ToString(@"yyyy-MM-dd\T" + timePM_2 + @":ss\Z");
                            p.id = ++id;
                            p.allDay = false;

                            idvs.Add(p);
                        }
                    }
                    //團體
                    else if (touristType == 2)
                    {
                        //上午
                        FullCalendarEventObj a = new FullCalendarEventObj();
                        bool openAM = true;
                        if (groupAMAny)
                        {
                            openAM = false;
                        }
                        //else
                        //{
                        //    var idvNo = (from row in rows
                        //                 where row.TouristType == 1 && row.ReservationBegin >= date && row.ReservationBegin < dateNext
                        //                 select row.TouristRange1).Sum();
                        //    if ((touristNo + idvNo) > slotMaxTourists)
                        //        openAM = false;
                        //}
                        if (openAM)
                        {
                            a.title = "上午：可預約";
                            a.className = "am ok";
                        }
                        else
                        {
                            a.title = "上午：已額滿";
                            a.className = "am full";
                        }
                        a.id = ++id;
                        a.allDay = true;
                        a.start = date.ToString(@"yyyy-MM-dd\THH:mm:ss\Z");

                        idvs.Add(a);

                        //下午                            
                        FullCalendarEventObj p = new FullCalendarEventObj();
                        bool openPM = true;

                        if (groupPMAny)
                        {
                            openPM = false;
                        }
                        //else
                        //{
                        //    var idvNo = (from row in rows
                        //                 where row.TouristType == 1 && row.ReservationBegin >= dateNext && row.ReservationBegin < dateTommorrow
                        //                 select row.TouristRange1).Sum();
                        //    if ((touristNo + idvNo) > slotMaxTourists)
                        //        openPM = false;
                        //}
                        if (openPM)
                        {
                            p.title = "下午：可預約";
                            p.className = "pm ok";
                        }
                        else
                        {
                            p.title = "下午：已額滿";
                            p.className = "pm full";
                        }
                        p.id = ++id;
                        p.allDay = true;
                        p.start = date.ToString(@"yyyy-MM-dd\THH:mm:ss\Z");

                        idvs.Add(p);
                    }
                }
                date = date.AddDays(1);
            }
            return Json(idvs, JsonRequestBehavior.AllowGet);            
        }
        /*
        ActionResult IdvInfo(int touristNo)
        {
            DateTime today = DateTime.Now.Date;
            DateTime endDate = today.AddDays(reservationMaxDays);

            DatabaseEntities entity = new DatabaseEntities();
            var rows = from row in entity.Reservation
                       where row.ReservationBegin >= today && row.ReservationBegin <= endDate
                       select row;

            var holidays = from row in entity.Holiday
                           //where row.Date >= today && row.Date <= endDate
                           where row.OffDate >= today
                           select row;

            JsonResult jr = new JsonResult();

            List<FullCalendarEventObj> idvs = new List<FullCalendarEventObj>();

            int id = 0;
            for (int i = 0; i < reservationMaxDays; i++)
            {
                DateTime date = today.AddDays(i);
                //只開放周五
                if (date.DayOfWeek != DayOfWeek.Friday)
                    continue;
                //例假休館日
                var offDays = (from row in holidays
                               where row.OffDate == date
                               select row).Any();
                if (offDays)
                    continue;               
                

                DateTime dateNext = date.AddHours(12);
                //上午
                FullCalendarEventObj a = new FullCalendarEventObj();
                bool openAM = true;
                var groupAMAny = (from row in rows
                                where row.TouristType == 2 && row.ReservationBegin >= date && row.ReservationBegin < dateNext
                                select row).Any();
                if (groupAMAny)
                {
                    openAM = false;
                }
                else
                {
                    var idvNo = (from row in rows
                                 where row.TouristType == 1 && row.ReservationBegin >= date && row.ReservationBegin < dateNext
                                 select row.TouristRange1).Sum();
                    if ((touristNo + idvNo) > slotMaxTourists)
                        openAM = false;
                }
                if (openAM)
                {
                    a.title = "上午：可預約";
                    a.className = "am ok";
                }
                else
                {
                    a.title = "上午：已額滿";
                    a.className = "am full";
                }
                a.id = ++id;
                a.allDay = true;
                a.start = date.ToString(@"yyyy-MM-dd\THH:mm:ss\Z");
                
                idvs.Add(a);

                //下午
                DateTime dateTommorrow = date.AddDays(1);
                FullCalendarEventObj p = new FullCalendarEventObj();
                bool openPM = true;
                var groupPMAny = (from row in rows
                                  where row.TouristType == 2 && row.ReservationBegin >= dateNext && row.ReservationBegin < dateTommorrow
                                  select row).Any();
                if (groupPMAny)
                {
                    openPM = false;
                }
                else
                {
                    var idvNo = (from row in rows
                                 where row.TouristType == 1 && row.ReservationBegin >= dateNext && row.ReservationBegin < dateTommorrow
                                 select row.TouristRange1).Sum();
                    if ((touristNo + idvNo) > slotMaxTourists)
                        openPM = false;
                }
                if (openPM)
                {
                    p.title = "下午：可預約";
                    p.className = "pm ok";
                }
                else
                {
                    p.title = "下午：已額滿";
                    p.className = "pm full";
                }
                p.id = ++id;
                p.allDay = true;
                p.start = date.ToString(@"yyyy-MM-dd\THH:mm:ss\Z");
                
                idvs.Add(p);
            }
            return Json(idvs, JsonRequestBehavior.AllowGet);
        }
        
        public ActionResult GroupOpen(int touristNo)
        {
            DateTime today = DateTime.Now.Date;
            DateTime endDate = today.AddDays(reservationMaxDays);
            DatabaseEntities entity = new DatabaseEntities();
            var rows = from row in entity.Reservation
                       where row.ReservationBegin >= today && row.ReservationBegin <= endDate
                       select row;

            JsonResult jr = new JsonResult();

            List<FullCalendarEventObj> idvs = new List<FullCalendarEventObj>();            
            int id = 0;
            for (int i = 0; i < reservationMaxDays; i++)
            {
                DateTime date = today.AddDays(i);
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                DateTime dateNext = date.AddDays(1);
                var rowDay = (from row in rows
                              where row.ReservationBegin >= date && row.ReservationBegin < dateNext
                              select row.TouristRange2).Sum();
                int max = dayMaxTourists - touristNo;
                if (rowDay != null)
                    max = max - rowDay.Value;


                FullCalendarEventObj a = new FullCalendarEventObj();                
                if (max > 0)
                    a.title = "團體：可預約(" + max + ")";
                else
                    a.title = "團體：已額滿";  
                a.id = ++id;
                a.allDay = true;
                a.start = date.ToString(@"yyyy-MM-dd\THH:mm:ss\Z");
                idvs.Add(a);
            }
            return Json(idvs, JsonRequestBehavior.AllowGet);
        }

        
        public ActionResult IndividualDayOpen(DateTime date,int touristNo)
        {
            DateTime nextDate = date.Date.AddDays(1);
            DatabaseEntities entity = new DatabaseEntities();
            var rows = from row in entity.Reservation
                       where row.ReservationBegin >= date && row.ReservationBegin <= nextDate
                       select row;

            JsonResult jr = new JsonResult();
            List<FullCalendarEventObj> idvs = new List<FullCalendarEventObj>();
            DateTime todayBegin = new DateTime(date.Year, date.Month, date.Day, dailyBeginHour, 0, 0);
            DateTime todayEnd = new DateTime(date.Year, date.Month, date.Day, dailyEndHour, 0, 0);
            DateTime todayBreak = new DateTime(date.Year, date.Month, date.Day, dailyBreakHour, 0, 0);
            DateTime d = todayBegin;
            int id = 0;
            while(d<todayEnd)
            {                
                var rowDay = (from row in rows
                              where row.ReservationBegin == d
                              select row.TouristRange2).Sum();
                int no=slotMaxTourists-touristNo;
                if (rowDay != null)
                    no = no - rowDay.Value;
                FullCalendarEventObj a = new FullCalendarEventObj();
                if (no >= 0)
                    a.title = "個人名額：" + no;
                else
                    a.title = "已額滿";
                a.id = ++id;
                a.allDay = false;
                a.start = d.ToString(@"yyyy-MM-dd\THH:mm:ss\Z");
                a.end = d.AddHours(1).ToString(@"yyyy-MM-dd\THH:mm:ss\Z");
                idvs.Add(a);

                d = d.AddHours(1);
                if (d == todayBreak)
                    d = d.AddHours(1);
            }
            return Json(idvs, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GroupDayOpen(DateTime date, int touristNo)
        {
            DateTime nextDate = date.Date.AddDays(1);
            DatabaseEntities entity = new DatabaseEntities();
            var rows = from row in entity.Reservation
                       where row.ReservationBegin >= date && row.ReservationBegin <= nextDate
                       select row;

            JsonResult jr = new JsonResult();
            List<FullCalendarEventObj> idvs = new List<FullCalendarEventObj>();
            DateTime todayBegin = new DateTime(date.Year, date.Month, date.Day, dailyBeginHour, 0, 0);
            DateTime todayEnd = new DateTime(date.Year, date.Month, date.Day, dailyEndHour, 0, 0);
            DateTime todayBreak = new DateTime(date.Year, date.Month, date.Day, dailyBreakHour, 0, 0);
            DateTime d = todayBegin;
            int id = 0;
            while (d < todayEnd)
            {
                var rowDay = (from row in rows
                              where row.ReservationBegin == d
                              select row.TouristRange2).Sum();
                int no = slotMaxTourists - touristNo;
                if (rowDay != null)
                    no = no - rowDay.Value;
                FullCalendarEventObj a = new FullCalendarEventObj();
                if (no >= 0)
                    a.title = "團體名額：" + no;
                else
                    a.title = "已額滿" + no;
                a.id = ++id;
                a.allDay = false;
                a.start = d.ToString(@"yyyy-MM-dd\THH:mm:ss\Z");
                a.end = d.AddHours(1).ToString(@"yyyy-MM-dd\THH:mm:ss\Z");
                idvs.Add(a);

                d = d.AddHours(1);
                if (d == todayBreak)
                    d = d.AddHours(1);
            }
            return Json(idvs, JsonRequestBehavior.AllowGet);
        }
        */


        private double ConvertToTimestamp(DateTime value)
        {
            //create Timespan by subtracting the value provided from
            //the Unix Epoch
            TimeSpan span = (value - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime());
            //return the total seconds (which is a UNIX timestamp)
            return (double)span.TotalSeconds;
        }
    }

    class FullCalendarEventObj {
        public int id { get; set; }
        public string title { get; set; }
        public bool allDay { get; set; }
        public string start { get; set; }
        public string end { get; set; }
        public string className { get; set; }
    }

}


