﻿namespace EasyErrorHandlingMvc.Handlers
{
	using System;
	using System.Diagnostics;
	using System.Net;
	using System.Web;
	using System.Web.Mvc;
	using EasyErrorHandlingMvc.Rendering;

	public class ActionInvokerWrapper : IActionInvoker
	{
		public ActionInvokerWrapper(IActionInvoker actionInvoker, ILogger logger)
		{
			ActionInvoker = actionInvoker;
			Logger = logger;
		}

		protected IActionInvoker ActionInvoker { get; set; }

		protected ILogger Logger { get; set; }

		public bool InvokeAction(ControllerContext controllerContext, string actionName)
		{
			try
			{
				if (ActionInvoker.InvokeAction(controllerContext, actionName))
				{
					return true;
				}
			}
			catch (Exception exception)
			{
				return HandleActionInvokingException(controllerContext, actionName, exception);
			}

			return HandleNotFound(controllerContext, actionName);
		}

		protected bool HandleActionInvokingException(ControllerContext controllerContext, string actionName,
			Exception exception)
		{
			if (!controllerContext.HttpContext.Items.Contains(Configuration.LoggedHttpContextItemName))
			{
				controllerContext.HttpContext.Items[Configuration.LoggedHttpContextItemName] = true;
				LogActionInvokingException(controllerContext, actionName, exception);
			}

			// If CustomErrors are disabled or action is child action / thrown while rendering by ErrorHandlingController,
			// rethrow to prevent recursive ErrorHandlingController calls
			if (!controllerContext.HttpContext.IsCustomErrorEnabled || controllerContext.RouteData.IsChildAction() ||
				controllerContext.RouteData.RouteDataIsErrorHandlingController())
			{
				throw exception;
			}

			ErrorHandlingController controller = DependencyResolver.Current.GetService<ErrorHandlingController>();
			controller.Execute(exception, controllerContext.RequestContext);

			return true;
		}

		protected bool HandleNotFound(ControllerContext controllerContext, string actionName)
		{
			string message = string.Format("Action \"{0}\" for Controller \"{1}\" not found.", actionName,
				controllerContext.Controller.GetType().Name);
			HttpException httpException = new HttpException((int)HttpStatusCode.NotFound, message);

			LogActionNotFoundException(controllerContext, actionName, httpException);

			// If CustomErrors are disabled or action is child action / thrown while rendering by ErrorHandlingController,
			// return to prevent recursive ErrorHandlingController calls
			if (!controllerContext.HttpContext.IsCustomErrorEnabled || controllerContext.RouteData.IsChildAction() ||
				controllerContext.RouteData.RouteDataIsErrorHandlingController())
			{
				return false;
			}

			ErrorHandlingController controller = DependencyResolver.Current.GetService<ErrorHandlingController>();
			controller.Execute(httpException, controllerContext.RequestContext);

			return true;
		}

		protected void LogActionInvokingException(ControllerContext controllerContext, string actionName, Exception exception)
		{
			string message =
				string.Format("[ActionInvokerWrapper]: Invoking of Action \"{0}\" for Controller \"{1}\" threw an exception.",
					actionName, controllerContext.Controller.GetType().Name);

			try
			{
				Logger.Log(message, exception, controllerContext.RequestContext);
			}
			catch (Exception)
			{
				Trace.WriteLine(message);
			}
		}

		protected void LogActionNotFoundException(ControllerContext controllerContext, string actionName,
			HttpException httpException)
		{
			string message = string.Format("[ActionInvokerWrapper]: Action \"{0}\" for Controller \"{1}\" not found.", actionName,
				controllerContext.Controller.GetType().Name);

			try
			{
				Logger.Log(message, httpException, controllerContext.RequestContext);
			}
			catch (Exception)
			{
				Trace.WriteLine(message);
			}
		}
	}
}