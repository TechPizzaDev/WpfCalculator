using System;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using WpfCalculator.Expressions;

namespace WpfCalculator
{
    public static class EErrorExtensions
    {
        public static bool ContainsError(this EError? error, string errorId)
        {
            if (error == null)
                return false;

            if (error.InnerError != null && error.InnerError.ContainsError(errorId))
                return true;

            return error.Id == errorId;
        }

        public static bool ContainsError(this EError? error, EErrorCode errorCode)
        {
            return ContainsError(error, errorCode.ToString());
        }

        public static bool IsError(this EError? error, string errorId)
        {
            if (error == null)
                return false;

            return error.Id == errorId;
        }

        public static bool IsError(this EError? error, EErrorCode errorCode)
        {
            return IsError(error, errorCode.ToString());
        }

        public static EError SetData(this EError error, string key, JToken value)
        {
            if (error == null)
                throw new ArgumentNullException(nameof(error));

            error.Data[key] = value;

            return error;
        }

        public static EError SetName(this EError error, string value)
        {
            return error.SetData("Name", value);
        }
    }
}
