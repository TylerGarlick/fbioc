using FlitBit.IoC.Meta;
using FlitBit.Wireup;
using FlitBit.Wireup.Meta;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlitBit.IoC.Tests.Examples
{
  public interface IBeNamed
  {
    string Name { get; }
  }

  [ContainerRegister(typeof(IBeNamed), RegistrationBehaviors.Default)]
  public class A : IBeNamed
  {
    public A() { this.Name = this.GetType().Name; }

    public string Name { get; protected set; }
  }

  public class B : IBeNamed
  {
    public B() { this.Name = this.GetType().Name; }

    public string Name { get; protected set; }
  }

  [TestClass]
  public class Example1
  {
    [TestMethod]
    public void CreateNew_CanCreateRegisteredInterface()
    {
      // Create the instance...
      var instance = Create.New<IBeNamed>();

      // Ensure it is what we think it is...
      Assert.IsInstanceOfType(instance, typeof(IBeNamed));
      Assert.IsInstanceOfType(instance, typeof(A));
      Assert.AreEqual("A", instance.Name);

      using (var create = Create.NewContainer())
      {
        create.Registry
              .ForType<IBeNamed>()
              .Register<B>();

        var b = create.New<IBeNamed>();
        Assert.AreEqual("B", b.Name);

      }
    }
  }
}
