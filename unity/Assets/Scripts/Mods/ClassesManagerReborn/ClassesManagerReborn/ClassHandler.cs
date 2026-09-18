using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ClassesManagerReborn
{
    /// <summary>
    /// Implement this class to have things happen during the class regestration preiod Init is called at the start of the regestration, and PostInit is called once all Inits have finished.
    /// </summary>
    public abstract class ClassHandler
    {
        public abstract IEnumerator Init();

        public virtual IEnumerator PostInit() { yield break; }
    }
}
