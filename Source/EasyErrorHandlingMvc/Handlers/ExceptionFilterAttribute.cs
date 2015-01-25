﻿namespace EasyErrorHandlingMvc.Handlers
{
	using System.Diagnostics;
	using System.Web.Mvc;
	using EasyErrorHandlingMvc.Rendering;

	public class ExceptionFilterAttribute : FilterAttribute, IExceptionFilter
	{
		public ExceptionFilterAttribute()
		{
			ErrorHandlingController = DependencyResolver.Current.GetService<ErrorHandlingController>();
			Logger = new NullLogger();
		}

		public ExceptionFilterAttribute(ILogger logger)
		{
			ErrorHandlingController = DependencyResolver.Current.GetService<ErrorHandlingController>();
			Logger = logger;
		}

		protected ErrorHandlingController ErrorHandlingController { get; set; }

		protected ILogger Logger { get; set; }

		public void OnException(ExceptionContext filterContext)
		{
			if (!filterContext.HttpContext.Items.Contains(Configuration.LoggedHttpContextItemName))
			{
				filterContext.HttpContext.Items[Configuration.LoggedHttpContextItemName] = true;
				LogException(filterContext);
			}

			// If CustomErrors are disabled or exception is handled or action is child action / thrown while rendering by ErrorHandlingController,
			// return to prevent recursive ErrorHandlingController calls
			if (!filterContext.HttpContext.IsCustomErrorEnabled || filterContext.ExceptionHandled || filterContext.IsChildAction ||
				filterContext.RouteData.RouteDataIsErrorHandlingController())
			{
				return;
			}

			HandleException(filterContext);
		}

		protected void HandleException(ExceptionContext filterContext)
		{
			filterContext.ExceptionHandled = true;

			ErrorHandlingController.Execute(filterContext.Exception, filterContext.RequestContext);
		}

		protected void LogException(ExceptionContext filterContext)
		{
			string message = string.Format(
				"[ExceptionFilterAttribute]: Action \"{0}\" of Controller \"{1}\" threw an exception.",
				filterContext.RouteData.Values["action"], filterContext.RouteData.Values["controller"]);

			try
			{
				Logger.Log(message, filterContext.Exception, filterContext.RequestContext);
			}
			catch
			{
				Trace.WriteLine(message);
			}
		}
	}
}