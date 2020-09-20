using System.Collections;

namespace Neo.IO.Actors
{
    internal interface IDropeable
    {
        bool ShallDrop(object message, IEnumerable queue);
    }
}
