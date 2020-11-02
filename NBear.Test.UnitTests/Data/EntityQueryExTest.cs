//in developping code
#if DEBUG

using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using NBear.Common;
using NBear.Data;
using Entities;

namespace NBear.Test.UnitTests.Data
{
    [TestClass]
    public class EntityQueryExTest
    {
        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 

        private Gateway gateway = null;

        [TestInitialize()]
        public void MyTestInitialize()
        {
            gateway = new Gateway("Northwind");
            gateway.RegisterSqlLogger(new LogHandler(Console.Write));
        }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        //tetst scripts for EntityQueryEx
        
        //--
        //Root objects
        //--
        //Gateway.From

        //-

        //unit tests

        //--
        //basic find
        //--
        //gateway.From<User>().Where(User._.Name == "teddy" & User._.ID > 0).ToArrayList<User>().Filter(User._.ID < 100);
        //gateway.From<User>().Where(User._.Name == "teddy" & User._.ID > 0).OrderBy(User._.Name.Desc & User._.ID.Asc).ToArrayList<User>(10);
        //gateway.From<User>().Where(User._.Name == "teddy" & User._.ID > 0).OrderBy(User._.Name.Desc & User._.ID.Asc).Count(User._.ID);

        [TestMethod]
        public void TestBasicFind()
        {
            //TestQuerySection query = new TestQuerySection(gateway.From<Order>(), Order._.OrderID > 0, Order._.OrderID.Desc);
            //Assert.IsTrue(query.ToArrayList<Order>().Filter(Order._.EmployeeID > 0).Length > 0);
            //Assert.IsTrue(query.ToArrayList<Order>(10).Count == 10);
            //Assert.IsTrue(query.Count(Order._.EmployeeID, true) > 0);
        }

        //--
        //group by
        //--
        //gateway.From<User>().GroupBy(User._.Status).Having(User._.ID > 0).Top(1).Select(User._.Status, User.ID.Count).ToSingle<UserGroupBySummary>();

        //--
        //join
        //--
        //gateway.From<User>().Join<Profile>(User._.ID == Profile._.ID).Where(User._.ID > 0 && Profile._.ID > 0).Select(User._.ID, User._.Name).ToArrayList<UserSummary>();

        //--
        //self join
        //--
        //gateway.From<User>("user1").Join<User>("user2", User._.Alias("user1").ParentID == User._.Alias("user2").ID).ToArrayList<UserExtended>();

        //--
        //extended original find
        //--
        //gateway.Find<User>(User_.Profile._.ID == 10);   => gateway.From<User>().Join<Profile>.On(User._.ID == Profile._.ID).Where(Profile._.ID == 10).Select<User>().ToSingle<User>();
    }
}

#endif