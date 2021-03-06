// Copyright 2004-2010 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Text.RegularExpressions;

namespace Castle.MonoRail.Framework.Tests.Helpers
{
	using System.Globalization;
	using System.IO;
	using System.Threading;

	using Castle.MonoRail.Framework.Helpers;
	using Castle.MonoRail.Framework.Tests.Controllers;
	
	using NUnit.Framework;

	[TestFixture]
	public class FormHelperCheckboxFieldListTestCase
	{
		class MockClass
		{
			public int[] Values { get; set; }
		}
		
		private FormHelper helper;
		private Product product;
		private SimpleUser user;
		private SimpleUser[] users;
		private Subscription subscription;
		private Month[] months;
		private MockClass mock;

		[SetUp]
		public void Init()
		{
			var en = CultureInfo.CreateSpecificCulture("en");

			Thread.CurrentThread.CurrentCulture	= en;
			Thread.CurrentThread.CurrentUICulture = en;

			helper = new FormHelper();

			subscription = new Subscription();
			mock = new MockClass();
			months = new[] {new Month(1, "January"), new Month(1, "February")};
			product = new Product("memory card", 10, (decimal) 12.30);
			user = new SimpleUser();
			users = new[] { new SimpleUser(1, false), new SimpleUser(2, true), new SimpleUser(3, false), new SimpleUser(4, true) };
			mock.Values = new[] { 2, 3 };

			var controller = new HomeController();
			var context = new ControllerContext();

			context.PropertyBag.Add("product", product);
			context.PropertyBag.Add("user", user);
			context.PropertyBag.Add("users", users);
			context.PropertyBag.Add("roles", new[] { new Role(1, "a"), new Role(2, "b"), new Role(3, "c") });
			context.PropertyBag.Add("sendemail", true);
			context.PropertyBag.Add("confirmation", "abc");
			context.PropertyBag.Add("fileaccess", FileAccess.Read);
			context.PropertyBag.Add("subscription", subscription);
			context.PropertyBag.Add("months", months);
			context.PropertyBag.Add("mock", mock);

			helper.SetController(controller, context);
		}

		/// <summary>
		/// Code posted on the mailing list
		/// </summary>
		[Test]
		public void BugReport1()
		{
			var list = 
				helper.CreateCheckboxList("mock.Values", new[] {1,2,3,4});
			
			Assert.IsNotNull(list);
			
			var index = 0;
			
			foreach(var item in list)
			{
				var content = list.Item();
				if (index == 1 || index == 2)
				{
					Assert.AreEqual("<input type=\"checkbox\" id=\"mock_Values_" + index +
						"_\" name=\"mock.Values[" + index + "]\" value=\"" + item + "\" checked=\"checked\" />", content);
				}
				else
				{
					Assert.AreEqual("<input type=\"checkbox\" id=\"mock_Values_" + index +
						"_\" name=\"mock.Values[" + index + "]\" value=\"" + item + "\" />", content);
				}
				index++;
			}

		}
		
		/// <summary>
		/// Tests the subscription.Months as null and an int array as source
		/// </summary>
		[Test]
		public void CheckboxFieldList1()
		{
			subscription.Months = null;
			
			var list = 
				helper.CreateCheckboxList("subscription.Months", new[] {1,2,3,4,5,6});
			
			Assert.IsNotNull(list);
			
			var index = 0;
			
			foreach(var item in list)
			{
				var content = list.Item();
				Assert.AreEqual("<input type=\"checkbox\" id=\"subscription_Months_" + index + "_\" name=\"subscription.Months[" + index + "]\" value=\"" + item + "\" />", content);
				index++;
			}
		}

		/// <summary>
		/// Tests the subscription.Months as non null and an int array as source
		/// </summary>
		[Test]
		public void CheckboxFieldList2()
		{
			subscription.Months = new[] { 1, 2, 3, 4, 5 };
			
			var list = 
				helper.CreateCheckboxList("subscription.Months", new[] {1,2,3,4,5});
			
			Assert.IsNotNull(list);
			
			var index = 0;

			foreach(var item in list)
			{
				var content = list.Item();
				Assert.AreEqual("<input type=\"checkbox\" id=\"subscription_Months_" + index + "_\" " + 
					"name=\"subscription.Months[" + index + "]\" value=\"" + item + "\" checked=\"checked\" />", content);
				index++;
			}
		}
		
		/// <summary>
		/// Tests the subscription.Months2 as null and using a <c>Month</c> array as data source
		/// </summary>
		[Test]
		public void CheckboxFieldList3()
		{
			var list = 
				helper.CreateCheckboxList("subscription.Months2", months, DictHelper.Create("value=id"));
			
			Assert.IsNotNull(list);

			var index = 0;

			foreach(Month item in list)
			{
				var content = list.Item();
				Assert.AreEqual("<input type=\"checkbox\" id=\"subscription_Months2_" + index +
					"_\" name=\"subscription.Months2[" + index + "].id\" value=\"" + item.Id + "\" />", content);
				index++;
			}
		}
		
		/// <summary>
		/// Tests the subscription.Months2 as null and using a <c>Month</c> array as data source
		/// </summary>
		[Test]
		public void CheckboxFieldList3a()
		{
			subscription.Months3 = null;
			
			var list = 
				helper.CreateCheckboxList("subscription.Months3", months, DictHelper.Create("value=id"));
			
			Assert.IsNotNull(list);

			var index = 0;

			foreach(Month item in list)
			{
				var content = list.Item();
				Assert.AreEqual("<input type=\"checkbox\" id=\"subscription_Months3_" + index +
					"_\" name=\"subscription.Months3[" + index + "].id\" value=\"" + item.Id + "\" />", content);
				index++;
			}
		}
		
		/// <summary>
		/// Tests the subscription.Months2 as non null and using a <c>Month</c> array as data source
		/// </summary>
		[Test]
		public void CheckboxFieldList4()
		{
			subscription.Months2 = new[] {new Month(3, "March")};
			
			var list = 
				helper.CreateCheckboxList("subscription.Months2", months, DictHelper.Create("value=id"));
			
			Assert.IsNotNull(list);

			var index = 0;

			foreach(Month item in list)
			{
				var content = list.Item();
				Assert.AreEqual("<input type=\"checkbox\" id=\"subscription_Months2_" + index +
					"_\" name=\"subscription.Months2[" + index + "].Id\" value=\"" + item.Id + "\" />", content);
				index++;
			}
		}

		/// <summary>
		/// Tests the subscription.Months2 as non null and using a <c>Month</c> array as data source
		/// but with selection
		/// </summary>
		[Test]
		public void CheckboxFieldList5()
		{
			subscription.Months2 = new[] {new Month(1, "January"), new Month(2, "Feb") };
			
			var list = 
				helper.CreateCheckboxList("subscription.Months2", months, DictHelper.Create("value=id"));
			
			Assert.IsNotNull(list);

			var index = 0;

			foreach(Month item in list)
			{
				var content = list.Item();
				Assert.AreEqual("<input type=\"checkbox\" id=\"subscription_Months2_" + index +
					"_\" name=\"subscription.Months2[" + index + "].Id\" value=\"" + item.Id + "\" checked=\"checked\" />", content);
				index++;
			}
		}

		[Test]
		public void UsingIdPerElement()
		{
			subscription.Months = new[] { 1, 2, 3, 4, 5 };

			var list =
				helper.CreateCheckboxList("subscription.Months", new[] { 1, 2 });

			Assert.IsNotNull(list);

			var index = 0;

			foreach(var item in list)
			{
				var content = list.Item("menu" + index);

				if (index < 2)
				{
					Assert.AreEqual("<input type=\"checkbox\" id=\"menu" + index + "\" name=\"subscription.Months[" + index + "]\" value=\"" + item + "\" checked=\"checked\" />", content);
				}
				else
				{
					Assert.AreEqual("<input type=\"checkbox\" name=\"subscription.Months[" + index + "]\" value=\"" + item + "\" id=\"menu" + index + "\" />", content);
				}
				index++;
			}
		}

		[Test]
		public void CheckboxFieldListInDotNet2()
		{
			var list =
				helper.CreateCheckboxList("subscription.Months4", months, DictHelper.Create("value=id"));

			Assert.IsNotNull(list);

			var index = 0;

			foreach(Month item in list)
			{
				var content = list.Item();
				Assert.AreEqual("<input type=\"checkbox\" id=\"subscription_Months4_" + index + "_\" name=\"subscription.Months4[" + index + "].id\" value=\"" + item.Id + "\" />", content);

				index++;
			}
		}

		[Test]
		public void CheckboxFieldListInDotNet2_WithSelection()
		{
			subscription.Months4.Add(new Month(1, "January"));
			subscription.Months4.Add(new Month(2, "Feb"));

			var list =
				helper.CreateCheckboxList("subscription.Months4", months, DictHelper.Create("value=id"));

			Assert.IsNotNull(list);

			var index = 0;

			foreach(Month item in list)
			{
				var content = list.Item();
				Assert.AreEqual("<input type=\"checkbox\" id=\"subscription_Months4_" + index + "_\" name=\"subscription.Months4[" + index + "].Id\" value=\"" + item.Id + "\" checked=\"checked\" />", content);
				index++;
			}
		}

		/// <summary>
		/// Test a simple checkbox list with LabelFor values
		/// </summary>
		[Test]
		public void CheckBoxFieldListWithLabelFor()
		{
			subscription.Months = new[] { 1, 2, 3, 4, 5 };

			var list =
				helper.CreateCheckboxList("subscription.Months", new[] { 1, 2, 3, 4, 5 });

			Assert.IsNotNull(list);

			foreach (var item in list)
			{
				var content = list.Item();
				var label = list.LabelFor(item.ToString());

				var checkboxIdMatch = new Regex("\\s+id=\"(?<value>[^\"]+)\"(?:\\s+|>)", RegexOptions.IgnoreCase).Match(content);
				var labelIdMatch = new Regex("\\s+for=\"(?<value>[^\"]+)\"(?:\\s+|>)", RegexOptions.IgnoreCase).Match(label);
				var labelContentMatch = new Regex("<label[^>]*>(?<value>[^<]*)</label>", RegexOptions.IgnoreCase).Match(label);

				Assert.AreEqual(checkboxIdMatch.Groups["value"].Value, labelIdMatch.Groups["value"].Value);
				Assert.AreEqual(item.ToString(), labelContentMatch.Groups["value"].Value);
			}			
		}

		/// <summary>
		/// Test a simple checkbox list with LabelFor values
		/// </summary>
		[Test]
		public void CheckBoxFieldListWithLabelForUsingCustomLabel()
		{
			subscription.Months = new[] { 1, 2, 3, 4, 5 };

			var list =
				helper.CreateCheckboxList("subscription.Months", new[] { 1, 2, 3, 4, 5 });

			Assert.IsNotNull(list);

			foreach (var item in list)
			{
				var content = list.Item();
				var label = list.LabelFor("Item "+item);

				var checkboxIdMatch = new Regex("\\s+id=\"(?<value>[^\"]+)\"(?:\\s+|>)", RegexOptions.IgnoreCase).Match(content);
				var labelIdMatch = new Regex("\\s+for=\"(?<value>[^\"]+)\"(?:\\s+|>)", RegexOptions.IgnoreCase).Match(label);
				var labelContentMatch = new Regex("<label[^>]*>(?<value>[^<]*)</label>", RegexOptions.IgnoreCase).Match(label);

				Assert.AreEqual(checkboxIdMatch.Groups["value"].Value, labelIdMatch.Groups["value"].Value);
				Assert.AreEqual("Item " + item, labelContentMatch.Groups["value"].Value);
			}
		}

		/// <summary>
		/// Tests the subscription.Months2 as non null and using a <c>Month</c> array as data source
		/// </summary>
		[Test]
		public void CheckboxFieldListWithHiddenField() {
			subscription.Months2 = new[] { new Month(3, "March") };

			var list =
				helper.CreateCheckboxList("subscription.Months2", months, DictHelper.Create("value=id"));

			Assert.IsNotNull(list);

			var index = 0;

			foreach (Month item in list) {
				var content = list.ItemAsHiddenField();
				Assert.AreEqual("<input type=\"hidden\" id=\"subscription_Months2_" + index +
					"_\" name=\"subscription.Months2[" + index + "]\" value=\"" + item.Id + "\" />", content);
				index++;
			}
		}
	}
}
