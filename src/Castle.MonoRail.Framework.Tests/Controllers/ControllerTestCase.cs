﻿// Copyright 2004-2010 Castle Project - http://www.castleproject.org/
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

namespace Castle.MonoRail.Framework.Tests.Controllers
{
	using System;
	using System.Web;
	using Castle.MonoRail.Framework.Services;
	using Core;
	using Descriptors;
	using NUnit.Framework;
	using Test;

	[TestFixture]
	public class ControllerTestCase
	{
		private StubEngineContext engineContext;
		private StubViewEngineManager engStubViewEngineManager;
		private StubMonoRailServices services;
		private StubResponse response;

		[SetUp]
		public void Init()
		{
			var request = new StubRequest();
			response = new StubResponse();
			services = new StubMonoRailServices();
			engStubViewEngineManager = new StubViewEngineManager();
			services.ViewEngineManager = engStubViewEngineManager;
			engineContext = new StubEngineContext(request, response, services, null);
		}

		[Test]
		public void ControllerCallsInitialize()
		{
			var controller = new ControllerWithInitialize();

			var context = new ControllerContext("controller", "", "action1", new ControllerMetaDescriptor());

			controller.Process(engineContext, context);

			Assert.IsTrue(controller.Initialized);
		}

		[Test, ExpectedException(typeof(MonoRailException), ExpectedMessage = "Could not find action named NonExistentAction on controller \\home")]
		[Ignore("Added inferred actions, so this test is no longer valid.")]
		public void InvokingNonExistingActionResultsIn404()
		{
			var controller = new ControllerAndViews();

			var context = new DefaultControllerContextFactory().
				Create("", "home", "NonExistentAction", new ControllerMetaDescriptor());

			try
			{
				controller.Process(engineContext, context);
			}
			catch(MonoRailException)
			{
				Assert.AreEqual(404, response.StatusCode);

				throw;
			}
		}

		[Test]
		public void RendersInferredViewIfTemplateExists()
		{
			var controller = new ControllerAndViews();

			var context = services.ControllerContextFactory.
				Create("", "home", "InferredAction", new ControllerMetaDescriptor());

			engStubViewEngineManager.RegisterTemplate("home\\InferredAction");

			controller.Process(engineContext, context);

			Assert.AreEqual(200, response.StatusCode);
			Assert.AreEqual("OK", response.StatusDescription);
			Assert.AreEqual("home\\InferredAction", engStubViewEngineManager.TemplateRendered);
		}

		[Test]
		public void RendersInferredView_IfTemplateDoesNotExistsResultsIn404()
		{
			var controller = new ControllerAndViews();

			var context = services.ControllerContextFactory.
				Create("", "home", "InferredAction", new ControllerMetaDescriptor());

			try
			{
				controller.Process(engineContext, context);
				Assert.Fail("Should have thrown a 404.");
			}
			catch (HttpException)
			{
				Assert.AreEqual(404, response.StatusCode);
			}
		}

		[Test]
		public void RendersViewByDefault()
		{
			var controller = new ControllerAndViews();

			var context = services.ControllerContextFactory.
				Create("", "home", "EmptyAction", new ControllerMetaDescriptor());

			controller.Process(engineContext, context);

			Assert.AreEqual(200, response.StatusCode);
			Assert.AreEqual("OK", response.StatusDescription);
			Assert.AreEqual("home\\EmptyAction", engStubViewEngineManager.TemplateRendered);
		}

		[Test]
		public void ControllerCanOverrideView()
		{
			var controller = new ControllerAndViews();

			var context = services.ControllerContextFactory.
				Create("", "home", "ActionWithViewOverride", new ControllerMetaDescriptor());

			controller.Process(engineContext, context);

			Assert.AreEqual(200, response.StatusCode);
			Assert.AreEqual("OK", response.StatusDescription);
			Assert.AreEqual("home\\SomethingElse", engStubViewEngineManager.TemplateRendered);
		}

		[Test]
		public void ControllerCanCancelView()
		{
			var controller = new ControllerAndViews();

			var context = services.ControllerContextFactory.
				Create("", "home", "CancelsTheView", new ControllerMetaDescriptor());

			controller.Process(engineContext, context);

			Assert.AreEqual(200, response.StatusCode);
			Assert.AreEqual("OK", response.StatusDescription);
			Assert.IsNull(engStubViewEngineManager.TemplateRendered);
		}

		[Test]
		public void DefaultActionIsRun_AttributeOnMethod()
		{
			var controller = new ControllerWithDefMethodOnAction();

			var context = services.ControllerContextFactory.
				Create("", "home", "index", services.ControllerDescriptorProvider.BuildDescriptor(controller));

			controller.Process(engineContext, context);

			Assert.IsTrue(controller.DefExecuted);
			Assert.AreEqual("home\\index", engStubViewEngineManager.TemplateRendered);
			Assert.AreEqual(200, response.StatusCode);
			Assert.AreEqual("OK", response.StatusDescription);
		}

		[Test]
		public void DefaultActionIsRun_AttributeOnClass()
		{
			var controller = new ControllerWithDefaultActionAttribute();

			var context = services.ControllerContextFactory.
				Create("", "home", "index", services.ControllerDescriptorProvider.BuildDescriptor(controller));

			controller.Process(engineContext, context);

			Assert.IsTrue(controller.DefExecuted);
			Assert.AreEqual("home\\index", engStubViewEngineManager.TemplateRendered);
			Assert.AreEqual(200, response.StatusCode);
			Assert.AreEqual("OK", response.StatusDescription);
		}

		[Test]
		public void AllServiceInterfacesAreInvokedForAHelperSoItIsContextualized()
		{
			var controller = new ControllerWithCustomHelper();

			var context = services.ControllerContextFactory.
				Create("", "home", "index", services.ControllerDescriptorProvider.BuildDescriptor(controller));

			engineContext.CurrentController = controller;
			engineContext.CurrentControllerContext = context;

			controller.Process(engineContext, context);

			var helper = (MyCustomHelper) context.Helpers["MyCustomHelper"];
			Assert.IsTrue(helper.Service1Invoked);
			Assert.IsTrue(helper.Service2Invoked);
			Assert.IsTrue(helper.SetContextInvoked);
			Assert.IsTrue(helper.SetControllerInvoked);
		}

		#region Controllers

		private class ControllerWithInitialize : Controller
		{
			private bool initialized;

			public override void Initialize()
			{
				initialized = true;
			}

			public bool Initialized
			{
				get { return initialized; }
			}

			public void Action1()
			{
			}
		}

		private class ControllerAndViews : Controller
		{
			public void EmptyAction()
			{
			}

			public void ActionWithViewOverride()
			{
				RenderView("SomethingElse");
			}

			public void CancelsTheView()
			{
				CancelView();
			}
		}

		private class ControllerWithDefMethodOnAction : Controller
		{
			private bool defExecuted;

			public void EmptyAction()
			{
			}

			[DefaultAction]
			public void Default()
			{
				defExecuted = true;
			}

			public bool DefExecuted
			{
				get { return defExecuted; }
			}
		}

		[DefaultAction("Default")]
		class ControllerWithDefaultActionAttribute : Controller
		{
			private bool defExecuted;

			public void Default()
			{
				defExecuted = true;
			}

			public bool DefExecuted
			{
				get { return defExecuted; }
			}
		}

		[Helper(typeof(MyCustomHelper))]
		class ControllerWithCustomHelper : Controller
		{
			public void Index()
			{
			}
		}

		class MyCustomHelper : IContextAware, IControllerAware, IServiceEnabledComponent, IMRServiceEnabled
		{
			private bool setContextInvoked;
			private bool setControllerInvoked;
			private bool service1Invoked;
			private bool service2Invoked;

			public bool SetContextInvoked
			{
				get { return setContextInvoked; }
			}

			public bool SetControllerInvoked
			{
				get { return setControllerInvoked; }
			}

			public bool Service1Invoked
			{
				get { return service1Invoked; }
			}

			public bool Service2Invoked
			{
				get { return service2Invoked; }
			}

			public void SetContext(IEngineContext context)
			{
				setContextInvoked = true;
				Assert.IsNotNull(context);
			}

			public void SetController(IController controller, IControllerContext context)
			{
				setControllerInvoked = true;
				Assert.IsNotNull(controller);
				Assert.IsNotNull(context);
			}

			public void Service(IServiceProvider provider)
			{
				service1Invoked = true;
				Assert.IsNotNull(provider);
			}

			public void Service(IMonoRailServices serviceProvider)
			{
				service2Invoked = true;
				Assert.IsNotNull(serviceProvider);
			}
		}

		#endregion
	}
}
