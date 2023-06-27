using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PackageRequest.Controllers
{
    public class ExperianRequestFileStore
    {
        private readonly ConcurrentQueue<string> _eiRequestFiles;
        public ExperianRequestFileStore()
        {
            _eiRequestFiles = new ConcurrentQueue<string>();
        }

        public void AddNewRequest(string filename)
        {
            _eiRequestFiles.Enqueue(filename);
        }

        public string ProcessRequest()
        {
            _eiRequestFiles.TryDequeue(out string filename);

            return filename;
        }

        public string PeekRequest() {
            _eiRequestFiles.TryPeek(out string filename);

            return filename;
        }
    }
}