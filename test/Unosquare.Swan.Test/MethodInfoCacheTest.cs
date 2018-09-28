﻿namespace Unosquare.Swan.Test.MethodInfoCacheTest
{
    using System;
    using NUnit.Framework;
    using Mocks;

    [TestFixture]
    public class Retrieve
    {
        [Test]
        public void SingleMethodWithType_ReturnsMethodInfo()
        {
            var methodInfo = Runtime.MethodInfoCache.Retrieve(typeof(MethodCacheMock), nameof(MethodCacheMock.SingleMethod));

            Assert.NotNull(methodInfo);
        }

        [Test]
        public void SingleMethodWithGenericType_ReturnsMethodInfo()
        {
            var methodInfo = Runtime.MethodInfoCache.Retrieve<MethodCacheMock>(nameof(MethodCacheMock.SingleMethod));

            Assert.NotNull(methodInfo);
        }
        
        [Test]
        public void MultiMethodWithGenericTypeAndParamType_ReturnsMethodInfo()
        {
            var methodInfo = Runtime.MethodInfoCache.Retrieve<MethodCacheMock>(nameof(MethodCacheMock.MultiMethod), typeof(int));

            Assert.NotNull(methodInfo);
        }

        [Test]
        public void AmbiguousMethodWithTypeNoParamType_ThrowsAmbiguousMatchException()
        {
            Assert.Throws<System.Reflection.AmbiguousMatchException>(() =>
                Runtime.MethodInfoCache.Retrieve(typeof(MethodCacheMock), nameof(MethodCacheMock.AmbiguousMethod)));
        }

        [Test]
        public void MultiMethodWithGenericTypeAndWithDifferentParamType_ReturnSameMethodInfo()
        {
            var methodInfoIntParam = Runtime.MethodInfoCache.Retrieve(typeof(MethodCacheMock), nameof(MethodCacheMock.MultiMethod), typeof(int));
            var methodInfoDecimalParam = Runtime.MethodInfoCache.Retrieve(typeof(MethodCacheMock), nameof(MethodCacheMock.MultiMethod), typeof(decimal));

            Assert.AreEqual(methodInfoIntParam, methodInfoDecimalParam);
        }
        
        [Test]
        public void MultiMethodWithAliasWithTypeAndWithDifferentParamType_ReturnDifferentMethodInfo()
        {
            var methodInfoIntParam = Runtime.MethodInfoCache.Retrieve<MethodCacheMock>(nameof(MethodCacheMock.MultiMethod), "multiintgeneric", typeof(int));
            var methodInfoDecimalParam = Runtime.MethodInfoCache.Retrieve<MethodCacheMock>(nameof(MethodCacheMock.MultiMethod), "multidecimalgeneric", typeof(decimal));

            Assert.AreNotEqual(methodInfoIntParam, methodInfoDecimalParam);
        }

        [Test]
        public void MultiMethodWithAliasWithGenericTypeAndWithDifferentParamType_ReturnDifferentMethodInfo()
        {
            var methodInfoIntParam = Runtime.MethodInfoCache.Retrieve(typeof(MethodCacheMock), nameof(MethodCacheMock.MultiMethod), "multiint", typeof(int));
            var methodInfoDecimalParam = Runtime.MethodInfoCache.Retrieve(typeof(MethodCacheMock), nameof(MethodCacheMock.MultiMethod), "multidecimal", typeof(decimal));

            Assert.AreNotEqual(methodInfoIntParam, methodInfoDecimalParam);
        }
        
        [Test]
        public void ContainsWithNullType_ThrowsError()
        {
            Assert.Catch<ArgumentNullException>(() => Runtime.MethodInfoCache.Contains(null));
        }
        
        [Test]
        public void RetrieveWithNullType_ThrowsError()
        {
            Assert.Catch<ArgumentNullException>(() => Runtime.MethodInfoCache.Retrieve(null));
        }
    }
}
