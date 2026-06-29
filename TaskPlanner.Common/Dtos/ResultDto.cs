using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskPlanner.Common.CommonDto
{
    public class ResultDto
    {
        public bool isSuccess { get; set; }
        public string message { get; set; }
        public string temp { get; set; }
    }

    public class ResultDto<T>
    {
        public bool isSuccess { get; set; }
        public string message { get; set; }
        public string temp { get; set; }
        public T data { get; set; }

    }


    public class ResultDto<T1, T2>
    {
        public bool isSuccess { get; set; }
        public string message { get; set; }
        public string temp { get; set; }

        public T1 data { get; set; }
        public T2 supplemantaryData { get; set; }

    }
}
