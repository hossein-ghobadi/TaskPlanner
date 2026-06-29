using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskPlanner.Common.CommonDto
{
    public class IdLabelDto
    {
        public long id { get; set; }
        public string label { get; set; }
        /// <summary>شناسه انگلیسی (مثلاً نام enum) — در صورت نبود مقدار، در JSON ارسال نمی‌شود.</summary>
        public string? name { get; set; }
    }

}
