using System;
using System.Collections.Generic;
using System.Text;

namespace ExpressYourself.Application.Interfaces
{
    public interface IValidator<T>
    {
        void Validate(T instance);
    }
}
