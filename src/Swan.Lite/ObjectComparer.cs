﻿using Swan.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Swan
{
    /// <summary>
    /// Represents a quick object comparer using the public properties of an object
    /// or the public members in a structure.
    /// </summary>
    public static class ObjectComparer
    {
        /// <summary>
        /// Compare if two variables of the same type are equal.
        /// </summary>
        /// <typeparam name="T">The type of objects to compare.</typeparam>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns><c>true</c> if the variables are equal; otherwise, <c>false</c>.</returns>
        public static bool AreEqual<T>(T left, T right) => AreEqual(left, right, typeof(T));

        /// <summary>
        /// Compare if two variables of the same type are equal.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <param name="targetType">Type of the target.</param>
        /// <returns>
        ///   <c>true</c> if the variables are equal; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">targetType.</exception>
        public static bool AreEqual(object left, object right, Type targetType)
        {
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));

            if (targetType.TypeInfo().IsBasicType)
                return Equals(left, right);

            return targetType.IsValueType || targetType.IsArray
                ? AreStructsEqual(left, right, targetType)
                : AreObjectsEqual(left, right, targetType);
        }

        /// <summary>
        /// Compare if two objects of the same type are equal.
        /// </summary>
        /// <typeparam name="T">The type of objects to compare.</typeparam>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns><c>true</c> if the objects are equal; otherwise, <c>false</c>.</returns>
        public static bool AreObjectsEqual<T>(T left, T right)
            where T : class
        {
            return AreObjectsEqual(left, right, typeof(T));
        }

        /// <summary>
        /// Compare if two objects of the same type are equal.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <param name="targetType">Type of the target.</param>
        /// <returns><c>true</c> if the objects are equal; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">targetType.</exception>
        public static bool AreObjectsEqual(object left, object right, Type targetType)
        {
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));

            var properties = targetType.Properties();

            foreach (var propertyTarget in properties)
            {
                if (propertyTarget.IsArray)
                {
                    var leftObj = left.ReadProperty(propertyTarget.PropertyName) as IEnumerable;
                    var rightObj = right.ReadProperty(propertyTarget.PropertyName) as IEnumerable;

                    if (!AreEnumerationsEquals(leftObj, rightObj))
                        return false;
                }
                else
                {
                    if (!Equals(left.ReadProperty(propertyTarget.PropertyName), right.ReadProperty(propertyTarget.PropertyName)))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Compare if two structures of the same type are equal.
        /// </summary>
        /// <typeparam name="T">The type of structs to compare.</typeparam>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns><c>true</c> if the structs are equal; otherwise, <c>false</c>.</returns>
        public static bool AreStructsEqual<T>(T left, T right)
            where T : struct
        {
            return AreStructsEqual(left, right, typeof(T));
        }

        /// <summary>
        /// Compare if two structures of the same type are equal.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <param name="targetType">Type of the target.</param>
        /// <returns>
        ///   <c>true</c> if the structs are equal; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">targetType.</exception>
        public static bool AreStructsEqual(object left, object right, Type targetType)
        {
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));

            var fields = new List<MemberInfo>(
                targetType.TypeInfo().Fields.Where(c => c.IsPublic)).Union(
                targetType.Properties().Where(c => c.HasPublicGetter).Select(c => c.PropertyInfo));

            foreach (var targetMember in fields)
            {
                switch (targetMember)
                {
                    case FieldInfo field:
                        if (!Equals(field.GetValue(left), field.GetValue(right)))
                            return false;
                        break;
                    case PropertyInfo property:
                        if (!Equals(left.ReadProperty(property.Name), right.ReadProperty(property.Name)))
                            return false;
                        break;
                }
            }

            return true;
        }

        /// <summary>
        /// Compare if two enumerables are equal.
        /// </summary>
        /// <typeparam name="T">The type of enums to compare.</typeparam>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// <c>true</c> if two specified types are equal; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// left
        /// or
        /// right.
        /// </exception>
        public static bool AreEnumerationsEquals<T>(T left, T right)
            where T : IEnumerable?
        {
            if (Equals(left, default(T)))
                throw new ArgumentNullException(nameof(left));

            if (Equals(right, default(T)))
                throw new ArgumentNullException(nameof(right));

            var leftEnumerable = left.Cast<object>().ToArray();
            var rightEnumerable = right.Cast<object>().ToArray();

            if (leftEnumerable.Length != rightEnumerable.Length)
                return false;

            for (var i = 0; i < leftEnumerable.Length; i++)
            {
                var leftEl = leftEnumerable[i];
                var rightEl = rightEnumerable[i];

                if (!AreEqual(leftEl, rightEl, leftEl.GetType()))
                {
                    return false;
                }
            }

            return true;
        }
    }
}