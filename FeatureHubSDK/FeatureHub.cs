using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using IO.FeatureHub.SSE.Model;
using Newtonsoft.Json;

// because dependent library does

namespace FeatureHubSDK
{

  public enum Readiness
  {
    /// <summary>
    /// NotReady - there have been no features delivered as yet.
    /// </summary>
    NotReady,
    /// <summary>
    /// Ready - the initial set of features has been delivered and we are now ready.
    /// </summary>
    Ready,
    /// <summary>
    /// The connection failed because the URL was wrong or some other failure even happened.
    /// </summary>
    Failed
  }




}
