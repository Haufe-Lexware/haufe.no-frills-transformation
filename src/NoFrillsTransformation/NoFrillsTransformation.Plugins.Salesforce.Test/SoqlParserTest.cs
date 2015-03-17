using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using NoFrillsTransformation.Plugins.Salesforce;

namespace NoFrillsTransformation.Plugins.Salesforce.Test
{
    [TestClass]
    public class SoqlParserTest
    {
        [TestMethod]
        public void CheckSimpleSoql()
        {
            var query = SfdcReaderFactory.ParseQuery("Select Id, Field1, Field2, Field3 From Account");
            Assert.AreEqual(query.Entity, "Account");
            Assert.AreEqual(query.FieldNames.Length, 4);
        }

        [TestMethod]
        public void CheckSoqlWithWhere()
        {
            var query = SfdcReaderFactory.ParseQuery("Select Id, Field1, Field2 , Field4 ,Field5 From Account Where Field1='woots'");
            Assert.AreEqual(query.Entity, "Account");
            Assert.AreEqual(query.FieldNames.Length, 5);
        }

        [TestMethod]
        public void CheckSoqlWithoutFromFails()
        {
            try
            {
                var query = SfdcReaderFactory.ParseQuery("Select Id, Field1, Field2 , Field4 ,Field5 Where Field1='woots'");
            }
            catch (Exception)
            {
                return;
            }
            Assert.Fail("Faulty query was not recognized.");
        }

        [TestMethod]
        public void CheckSoqlWithoutSelectFails()
        {
            try
            {
                var query = SfdcReaderFactory.ParseQuery("Id, Field1, Field2 , Field4 ,Field5 From Opportunities");
            }
            catch (Exception)
            {
                return;
            }
            Assert.Fail("Faulty query was not recognized.");
        }
    }
}
