using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;

/// <summary>
///  Program, which serializes the object into XML data. Instead of using Reflection this approach uses
///  Multicast delegate.
/// </summary>


namespace Serializer {

    public delegate void PropertySerializer<Instance>(TextWriter writer, Instance instance);
    public delegate Value PropertyGetter<Instance, Value>(Instance instance);

    public class RootDescriptor<Instance> {

        private PropertySerializer<Instance> propertySerializer;

        public void Serialize(TextWriter writer, Instance instance, string tag) {

            writer.WriteLine($"<{tag}>");
            propertySerializer(writer, instance);
            writer.WriteLine($"<{tag}>");
        }
        /// <summary>
        /// End of the recursion. If we access just object without any sub-objects, it's going to just print the object.
        /// </summary>
        /// <typeparam name="Value"></typeparam>
        /// <param name="getter"></param>
        /// <param name="tag"></param>
        public void AccessLeaf<Value>(PropertyGetter<Instance, Value> getter, string tag) {

            propertySerializer += (TextWriter writer, Instance instance) => {
                writer.WriteLine($"<{tag}>{getter(instance)}</{tag}>");
            };
        }
        /// <summary>
        /// Adds a child delegate to a parent delegate. When we're going to call the RootDesc, delegates're going to be called in the order,
        /// which we add them to other delegate (Multicast delegate).
        /// </summary>
        /// <typeparam name="SubProperty"></typeparam>
        /// <param name="childDescriptor"></param>
        /// <param name="getter"></param>
        /// <param name="tag"></param>
        public void AccessChild<SubProperty>(RootDescriptor<SubProperty> childDescriptor,
            PropertyGetter<Instance, SubProperty> getter, string tag) {

            propertySerializer += (TextWriter writer, Instance instance) => {
                childDescriptor.Serialize(writer, getter(instance), tag);
            };
        }

    }

    class Address {
        public string Street { get; set; }
        public string City { get; set; }
    }

    class Country {
        public string Name { get; set; }
        public int AreaCode { get; set; }
    }

    class PhoneNumber {
        public Country Country { get; set; }
        public int Number { get; set; }
    }

    class Person {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Address HomeAddress { get; set; }
        public Address WorkAddress { get; set; }
        public Country CitizenOf { get; set; }
        public PhoneNumber MobilePhone { get; set; }
    }

    class Program {
        static void Main(string[] args) {
            RootDescriptor<Person> rootDesc = GetPersonDescriptor();

            var czechRepublic = new Country { Name = "Czech Republic", AreaCode = 420 };
            var person = new Person {
                FirstName = "Pavel",
                LastName = "Jezek",
                HomeAddress = new Address { Street = "Patkova", City = "Prague" },
                WorkAddress = new Address { Street = "Malostranske namesti", City = "Prague" },
                CitizenOf = czechRepublic,
                MobilePhone = new PhoneNumber { Country = czechRepublic, Number = 123456789 }
            };

            rootDesc.Serialize(Console.Out, person, "Person");

        }

        static RootDescriptor<Person> GetPersonDescriptor() {
            var rootDesc = new RootDescriptor<Person>();

            var addressDesc = new RootDescriptor<Address>();
            addressDesc.AccessLeaf((Address a) => { return a.Street; }, "Street");
            addressDesc.AccessLeaf((Address a) => { return a.City; }, "City");

            var countryDesc = new RootDescriptor<Country>();
            countryDesc.AccessLeaf((Country c) => { return c.Name; }, "Name");
            countryDesc.AccessLeaf((Country c) => { return c.AreaCode; }, "AreaCode");

            var phoneDesc = new RootDescriptor<PhoneNumber>();
            phoneDesc.AccessChild(countryDesc, (PhoneNumber p) => { return p.Country; }, "Country");
            phoneDesc.AccessLeaf((PhoneNumber p) => { return p.Number; }, "Number");

            rootDesc.AccessLeaf((Person p) => { return p.FirstName; }, "FirstName");
            rootDesc.AccessLeaf((Person p) => { return p.LastName; }, "LastName");
            rootDesc.AccessChild(addressDesc, (Person p) => { return p.HomeAddress; }, "HomeAddress");
            rootDesc.AccessChild(addressDesc, (Person p) => { return p.WorkAddress; }, "WorkAdress");
            rootDesc.AccessChild(countryDesc, (Person p) => { return p.CitizenOf; }, "CitizenOf");
            rootDesc.AccessChild(phoneDesc, (Person p) => { return p.MobilePhone; }, "MobilePhone");

            return rootDesc;
        }
    }
}
