﻿namespace EasyErrorHandlingMvc.Samples.Mvc5Extended.Core.Unity
{
	using Microsoft.Practices.Unity;
	using Microsoft.Practices.Unity.ObjectBuilder;

	public class SimpleErrorHandlingNLogAdapterExtension : UnityContainerExtension
	{
		protected override void Initialize()
		{
			Context.Strategies.AddNew<CreationStackTrackerStrategy>(UnityBuildStage.TypeMapping);
			Context.Strategies.AddNew<SimpleErrorHandlingNLogAdapterStrategy>(UnityBuildStage.TypeMapping);
		}
	}
}