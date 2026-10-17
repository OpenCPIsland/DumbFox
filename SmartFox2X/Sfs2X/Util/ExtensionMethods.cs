using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Sfs2X.Util
{
	public static class ExtensionMethods
	{
		public static TaskAwaiter GetAwaiter()
		{
			TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
			return ((Task)tcs.Task).GetAwaiter();
		}
	}
}
