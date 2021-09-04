﻿using System;
using System.Globalization;
using System.Reflection;

namespace Swan.Reflection
{
    internal sealed class TryParseMethodInfo : CommonMethodInfo
    {
        public TryParseMethodInfo(ITypeProxy typeInfo)
            : base(typeInfo, nameof(byte.TryParse))
        {
            // placeholder
        }

        protected override MethodInfo? RetriveMethodInfo(ITypeProxy typeInfo, string methodName) =>
            typeInfo.UnderlyingType.GetMethod(MethodName,
                new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeInfo.UnderlyingType.MakeByRefType() }) ??
            typeInfo.UnderlyingType.GetMethod(MethodName,
                new[] { typeof(string), typeInfo.UnderlyingType.MakeByRefType() });
    }
}