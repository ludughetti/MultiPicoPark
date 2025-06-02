using Fusion;

namespace Network
{
    public struct NetworkMoveInputData : INetworkInput
    {
        private byte _buttonsPressed;
        
        public void AddInput(NetworkMoveInputType inputType)
        {
            var flag = (byte)(1 << (int)inputType);
            _buttonsPressed |= flag;
        }
        
        public readonly bool IsInputDown(NetworkMoveInputType inputType)
        {
            var flag = (byte)(1 << (int)inputType);
            return (_buttonsPressed & flag) != 0;
        }
    }
}