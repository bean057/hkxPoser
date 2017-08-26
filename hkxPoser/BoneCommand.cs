using SharpDX;

namespace hkxPoser
{
    /// ����������܂��B
    public interface ICommand
    {
        /// ���ɖ߂��B
        void Undo();

        /// ��蒼���B
        void Redo();

        /// ���s����B
        bool Execute();
    }

    /// bone����
    public struct BoneAttr
    {
        /// ��]
        public Quaternion rotation;
        /// �ړ�
        public Vector3 translation;
    }

    /// bone����
    public class BoneCommand : ICommand
    {
        //����Ώ�bone
        hkaBone bone = null;
        /// �ύX�O�̑���
        BoneAttr old_attr;
        /// �ύX��̑���
        BoneAttr new_attr;

        /// bone����𐶐����܂��B
        public BoneCommand(hkaBone bone)
        {
            this.bone = bone;
            this.old_attr.rotation = bone.local.rotation;
            this.old_attr.translation = bone.local.translation;
        }

        /// ���ɖ߂��B
        public void Undo()
        {
            bone.local.rotation = old_attr.rotation;
            bone.local.translation = old_attr.translation;
        }

        /// ��蒼���B
        public void Redo()
        {
            bone.local.rotation = new_attr.rotation;
            bone.local.translation = new_attr.translation;
        }

        /// ���s����B
        public bool Execute()
        {
            this.new_attr.rotation = bone.local.rotation;
            this.new_attr.translation = bone.local.translation;
            bool updated = old_attr.rotation != new_attr.rotation || old_attr.translation != new_attr.translation;
            return updated;
        }
    }
}
