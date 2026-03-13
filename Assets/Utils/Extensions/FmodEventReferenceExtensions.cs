#region

using FMODUnity;

#endregion

namespace Utils.Extensions
{
    public static class FmodEventReferenceExtensions
    {
        public static string GetPath(this EventReference _eventReference)
        {
            string _path;
            RuntimeManager.StudioSystem.lookupPath(_eventReference.Guid, out _path);
            return _path;
        }
        
        public static void TryPlayOneShot(this EventReference _eventReference)
        {
            if (!string.IsNullOrEmpty(_eventReference.GetPath()))
            {
                RuntimeManager.PlayOneShot(_eventReference.GetPath());
            }
        }
    }
}