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

namespace Castle.MonoRail.Views.Brail.Tests
{
	using System;
	using System.Collections;
	using System.IO;
	using System.Reflection;
	using Castle.MonoRail.Framework.Descriptors;
	using Castle.MonoRail.Framework.Helpers;
	using Castle.MonoRail.Framework.JSGeneration;
	using Castle.MonoRail.Framework.JSGeneration.Prototype;
	using Castle.MonoRail.Framework.Resources;
	using Castle.MonoRail.Framework.Services;
	using Castle.MonoRail.Framework.Test;
	using Framework;
	using NUnit.Framework;

	public abstract class BaseViewOnlyTestFixture
	{
		protected string viewSourcePath;
		protected ControllerContext ControllerContext;
		protected HelperDictionary Helpers;
		private string lastOutput;
		protected string[] Layouts;
		protected StubEngineContext StubEngineContext;
		protected Hashtable PropertyBag;
		protected string Area = null;
		protected string ControllerName = "test_controller";
		protected string Action = "test_action";
		protected DefaultViewComponentFactory ViewComponentFactory;
		protected BooViewEngine BooViewEngine;

		protected string Layout
		{
			set { Layouts = new[] { value }; }
		}

		public BaseViewOnlyTestFixture()
			: this(ViewLocations.TestSiteBrail)
		{
		}

		public BaseViewOnlyTestFixture(string viewSource)
		{
			viewSourcePath = viewSource;
		}


		public string ViewSourcePath
		{
			get { return viewSourcePath; }
		}

		[SetUp]
		public void SetUp()
		{
			Layout = null;
			PropertyBag = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
			Helpers = new HelperDictionary();
			var services = new StubMonoRailServices 
			{ 
				UrlBuilder = new DefaultUrlBuilder(new StubServerUtility(), new StubRoutingEngine()), 
				UrlTokenizer = new DefaultUrlTokenizer()
			};
			var urlInfo = new UrlInfo(
				"example.org", "test", "/TestBrail", "http", 80,
				"http://test.example.org/test_area/test_controller/test_action.tdd",
				Area, ControllerName, Action, "tdd", "no.idea");
			StubEngineContext = new StubEngineContext(new StubRequest(), new StubResponse(), services,
													  urlInfo);
			StubEngineContext.AddService<IUrlBuilder>(services.UrlBuilder);
			StubEngineContext.AddService<IUrlTokenizer>(services.UrlTokenizer);

			ViewComponentFactory = new DefaultViewComponentFactory();
			ViewComponentFactory.Service(StubEngineContext);

			StubEngineContext.AddService<IViewComponentFactory>(ViewComponentFactory);
			ControllerContext = new ControllerContext
			{
				Helpers = Helpers, 
				PropertyBag = PropertyBag
			};
			StubEngineContext.CurrentControllerContext = ControllerContext;


			Helpers["formhelper"] = Helpers["form"] = new FormHelper(StubEngineContext);
			Helpers["urlhelper"] = Helpers["url"] = new UrlHelper(StubEngineContext);
			Helpers["dicthelper"] = Helpers["dict"] = new DictHelper(StubEngineContext);
			Helpers["DateFormatHelper"] = Helpers["DateFormat"] = new DateFormatHelper(StubEngineContext);

			var viewPath = Path.Combine(viewSourcePath, "Views");

			var loader = new FileAssemblyViewSourceLoader(viewPath);
			loader.AddAssemblySource(
				new AssemblySourceInfo(Assembly.GetExecutingAssembly().FullName,
									   "Castle.MonoRail.Views.Brail.Tests.ResourcedViews"));

			BooViewEngine = new BooViewEngine
			{
				Options = new BooViewEngineOptions
				{
					SaveDirectory = Environment.CurrentDirectory,
					SaveToDisk = false,
					Debug = true,
					BatchCompile = false
				}
			};

			BooViewEngine.SetViewSourceLoader(loader);
			BooViewEngine.Initialize();

			BeforEachTest();
		}

		protected virtual void BeforEachTest()
		{

		}


		public void ProcessView_StripRailsExtension(string url)
		{
			ProcessView(url.Replace(".rails", ""));
		}

		protected string ProcessView(string templatePath)
		{
			var sw = new StringWriter();
			ControllerContext.LayoutNames = Layouts;
			StubEngineContext.CurrentControllerContext = ControllerContext;
			BooViewEngine.Process(templatePath, sw, StubEngineContext, null, ControllerContext);
			lastOutput = sw.ToString();
			return lastOutput;
		}

		protected string ProcessViewJS(string templatePath)
		{
			var sw = new StringWriter();
			ControllerContext.LayoutNames = Layouts;
			StubEngineContext.CurrentControllerContext = ControllerContext;
			var engineManager = new DefaultViewEngineManager();
			engineManager.RegisterEngineForView(BooViewEngine);
			engineManager.RegisterEngineForExtesionLookup((BooViewEngine));
			var codeGenerator =
				  new JSCodeGenerator(StubEngineContext.Server,
					  engineManager,
					  StubEngineContext, null, ControllerContext, StubEngineContext.Services.UrlBuilder);

			IJSGenerator jsGen = new PrototypeGenerator(codeGenerator);

			codeGenerator.JSGenerator = jsGen;

			var info = new JSCodeGeneratorInfo(codeGenerator, jsGen,
				new object[] { new ScriptaculousExtension(codeGenerator) },
				new object[] { new ScriptaculousExtension(codeGenerator) });

			BooViewEngine.GenerateJS(templatePath, sw, info, StubEngineContext, null, ControllerContext);
			lastOutput = sw.ToString();
			return lastOutput;
		}

		protected void AddResource(string name, string resourceName, Assembly asm)
		{
			IResourceFactory resourceFactory = new DefaultResourceFactory();
			var descriptor = new ResourceDescriptor(
				null,
				name,
				resourceName,
				null,
				null);
			var resource = resourceFactory.Create(
				descriptor,
				asm);
			ControllerContext.Resources.Add(name, resource);
		}

		protected string RenderStaticWithLayout(string staticText)
		{
			ControllerContext.LayoutNames = Layouts;
			StubEngineContext.CurrentControllerContext = ControllerContext;

			BooViewEngine.RenderStaticWithinLayout(staticText, StubEngineContext, null, ControllerContext);
			lastOutput = ((StringWriter)StubEngineContext.Response.Output)
				.GetStringBuilder().ToString();
			return lastOutput;
		}

		public void AssertReplyEqualTo(string expected)
		{
			Assert.AreEqual(expected, lastOutput);
		}

		public void AssertReplyContains(string contained)
		{
			StringAssert.Contains(contained, lastOutput);
		}
	}
}