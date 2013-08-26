#region

using System;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;

#endregion

namespace Bsw.FayeDotNet.Test
{
    public class BaseTest
    {
        [SetUp]
        public virtual void SetUp()
        {
        }

        [TearDown]
        public virtual void Teardown()
        {
        }
    }
}