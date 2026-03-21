

#nullable enable
using IO.FeatureHub.SSE.Model;

namespace FeatureHubSDK
{
    // any class that implements this interface will be called when a user requests the value of a 
    // feature. If it intercepts because it has a value, it should return true,value. If it is not
    // it should return false,null - the value doesn't matter. It can return null when the feature is
    // null, but obviously shouldn't for bool values. The user may request a key that does not exist
    // and as such, there will be no featureState to be passed.
    //
    // the FeatureRepository holds these interceptors.
    public interface IFeatureValueInterceptor
    {
        // having been allowed to call, is it overridden?
        (bool, object?) GetValue(string key, IFeatureRepositoryContext repository, FeatureState? featureState);
    }
};

