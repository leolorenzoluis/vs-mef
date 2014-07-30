namespace Microsoft.VisualStudio.Composition
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection.Metadata;
    using System.Text;
    using System.Threading.Tasks;

    internal static class MetadataReaderExtensions
    {
        internal static Handle GetAttributeTypeHandle(this MetadataReader metadataReader, CustomAttribute customAttribute)
        {
            Handle attributeTypeHandle;
            var ctor = customAttribute.Constructor;
            if (ctor.HandleType == HandleType.MemberReference)
            {
                var memberReferenceHandle = (MemberReferenceHandle)ctor;
                var memberReference = metadataReader.GetMemberReference(memberReferenceHandle);
                attributeTypeHandle = memberReference.Parent;
            }
            else
            {
                MethodHandle methodHandle = (MethodHandle)ctor;
                var method = metadataReader.GetMethod(methodHandle);
                attributeTypeHandle = metadataReader.GetDeclaringType(methodHandle);
            }

            return attributeTypeHandle;
        }
    }
}
