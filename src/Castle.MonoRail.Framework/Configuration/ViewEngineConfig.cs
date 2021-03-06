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

namespace Castle.MonoRail.Framework.Configuration
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.IO;
	using System.Xml;
	using Castle.MonoRail.Framework.Internal;

	/// <summary>
	/// Represents the view engines configuration
	/// </summary>
	public class ViewEngineConfig : ISerializedConfig
	{
		private string viewPathRoot;
		private string virtualPathRoot;
		private List<String> pathSources = new List<String>();
		private List<AssemblySourceInfo> assemblySources = new List<AssemblySourceInfo>();
		private readonly List<ViewEngineInfo> viewEngines = new List<ViewEngineInfo>();

		/// <summary>
		/// Initializes a new instance of the <see cref="ViewEngineConfig"/> class.
		/// </summary>
		public ViewEngineConfig()
		{
			viewPathRoot = virtualPathRoot = "views";
		}

		#region ISerializedConfig implementation

		/// <summary>
		/// Deserializes the specified section.
		/// </summary>
		/// <param name="section">The section.</param>
		public void Deserialize(XmlNode section)
		{
			var engines = (XmlElement)section.SelectSingleNode("viewEngines");

			if (engines != null)
			{
				ConfigureMultipleViewEngines(engines);
			}
			else
			{
				// Backward compatibility

				ConfigureSingleViewEngine(section);
			}

			LoadAdditionalSources(section);
			ResolveViewPath();
		}

		#endregion

		/// <summary>
		/// Gets or sets the view path root.
		/// </summary>
		/// <value>The view path root.</value>
		public string ViewPathRoot
		{
			get { return viewPathRoot; }
			set { viewPathRoot = value; }
		}

		/// <summary>
		/// Gets or sets the virtual path root.
		/// </summary>
		/// <value>The virtual path root.</value>
		public string VirtualPathRoot
		{
			get { return virtualPathRoot; }
			set { virtualPathRoot = value; }
		}

		/// <summary>
		/// Gets the view engines.
		/// </summary>
		/// <value>The view engines.</value>
		public List<ViewEngineInfo> ViewEngines
		{
			get { return viewEngines; }
		}

		/// <summary>
		/// Gets or sets the additional assembly sources.
		/// </summary>
		/// <value>The sources.</value>
		public List<AssemblySourceInfo> AssemblySources
		{
			get { return assemblySources; }
			set { assemblySources = value; }
		}

		/// <summary>
		/// Gets or sets the path sources.
		/// </summary>
		/// <value>The path sources.</value>
		public List<string> PathSources
		{
			get { return pathSources; }
			set { pathSources = value; }
		}

		/// <summary>
		/// Sets the view directory to a path relative to the base directory of the current
		/// app domain.
		/// </summary>
		/// <param name="dir">The dir.</param>
		/// <returns>
		///  A reference to this <see cref="ViewEngineConfig"/> for method chaining.
		/// </returns>
		/// <remarks>
		///   This method is a shortcut for
		/// <code>
		/// ViewPathRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dir);
		/// </code>
		/// </remarks>
		public ViewEngineConfig SetRelativeViewDirectory(string dir)
		{
			ViewPathRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dir);
			return this;
		}

		/// <summary>
		/// Registers a view engine at this <see cref="ViewEngineConfig"/>
		/// </summary>
		/// <typeparam name="TViewEngine">The type of the view engine.</typeparam>
		/// <param name="xhtmlRendering"><c>true</c> if the view engine generates XHTML; 
		/// otherwise, <c>false</c>.</param>
		/// <returns>
		///  A reference to this <see cref="ViewEngineConfig"/> for method chaining.
		/// </returns>
		/// <remarks>
		/// This method is a shortcut for 
		/// <code>
		/// ViewEngines.Add(new ViewEngineInfo(typeof(TViewEngine), xhtmlRendering));
		/// </code>
		/// <para/>
		/// <typeparamref name="TViewEngine"/> must be a type that implements <see cref="IViewEngine"/>
		/// </remarks>
		public ViewEngineConfig AddViewEngine<TViewEngine>(bool xhtmlRendering)
		  where TViewEngine : IViewEngine
		{
			ViewEngines.Add(new ViewEngineInfo(typeof(TViewEngine), xhtmlRendering));
			return this;
		}

		/// <summary>
		/// Configures the default view engine.
		/// </summary>
		public void ConfigureDefaultViewEngine()
		{
			var engineType = typeof(Views.Aspx.WebFormsViewEngine);

			viewEngines.Add(new ViewEngineInfo(engineType, false));
		}

		private void ConfigureMultipleViewEngines(XmlElement engines)
		{
			viewPathRoot = engines.GetAttribute("viewPathRoot");

			if (string.IsNullOrEmpty(viewPathRoot))
			{
				viewPathRoot = "views";
			}

			foreach (XmlElement addNode in engines.SelectNodes("add"))
			{
				var typeName = addNode.GetAttribute("type");
				var xhtmlVal = addNode.GetAttribute("xhtml");

				if (string.IsNullOrEmpty(typeName))
				{
					const string message = "The attribute 'type' is required for the element 'add' under 'viewEngines'";
					throw new ConfigurationErrorsException(message);
				}

				var engine = TypeLoadUtil.GetType(typeName, true);

				if (engine == null)
				{
					var message = "The type '" + typeName + "' could not be loaded";
					throw new ConfigurationErrorsException(message);
				}

				viewEngines.Add(new ViewEngineInfo(engine, xhtmlVal == "true"));
			}

			if (viewEngines.Count == 0)
			{
				ConfigureDefaultViewEngine();
			}
		}

		private void ResolveViewPath()
		{
			if (!Path.IsPathRooted(viewPathRoot))
			{
				virtualPathRoot = viewPathRoot;
				viewPathRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, viewPathRoot);
			}

			if (!Directory.Exists(viewPathRoot))
			{
				throw new MonoRailException("View folder configured could not be found. " +
											"Check (or add) a viewPathRoot attribute to the viewEngines node on the MonoRail configuration (web.config)");
			}
		}

		private void ConfigureSingleViewEngine(XmlNode section)
		{
			section = section.SelectSingleNode("viewEngine");

			if (section == null)
			{
				ConfigureDefaultViewEngine();

				return;
			}

			var viewPath = section.Attributes["viewPathRoot"];

			if (viewPath == null)
			{
				viewPathRoot = virtualPathRoot = "views";
			}
			else
			{
				viewPathRoot = virtualPathRoot = viewPath.Value;
			}

			var xhtmlRendering = section.Attributes["xhtmlRendering"];

			var enableXhtmlRendering = false;

			if (xhtmlRendering != null)
			{
				try
				{
					enableXhtmlRendering = xhtmlRendering.Value.ToLowerInvariant() == "true";
				}
				catch (FormatException ex)
				{
					const string message = "The xhtmlRendering attribute of the views node must be a boolean value.";
					throw new ConfigurationErrorsException(message, ex);
				}
			}

			var customEngineAtt = section.Attributes["customEngine"];

			var engineType = typeof(Views.Aspx.WebFormsViewEngine);

			if (customEngineAtt != null)
			{
				engineType = TypeLoadUtil.GetType(customEngineAtt.Value);
			}

			viewEngines.Add(new ViewEngineInfo(engineType, enableXhtmlRendering));
		}

		private void LoadAdditionalSources(XmlNode section)
		{
			foreach (XmlElement assemblyNode in section.SelectNodes("/monorail/*/additionalSources/assembly"))
			{
				var assemblyName = assemblyNode.GetAttribute("name");
				var ns = assemblyNode.GetAttribute("namespace");

				assemblySources.Add(new AssemblySourceInfo(assemblyName, ns));
			}

			foreach (XmlElement pathNode in section.SelectNodes("/monorail/*/additionalSources/path"))
			{
				var pathName = pathNode.GetAttribute("location");

				pathSources.Add(pathName);
			}
		}
	}
}