using System;

namespace Aire.Helpers
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class EntityTableAttribute : Attribute
	{
		public string TableName { get; private set; }

		public EntityTableAttribute(string tableName)
		{
			TableName = tableName;
		}
	}
}