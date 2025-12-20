using DenOfIz;

namespace Graphics.Shaders;

public class RootSignatureBuilder(LogicalDevice logicalDevice)
{
    private readonly RootSignatureDesc _rootSignatureDesc = new();
    private List<ResourceBindingDesc> _bindings = [];

    public class BindingDesc<TSelf, TContinuation> where TSelf : BindingDesc<TSelf, TContinuation>
    {
        protected TSelf Self => (TSelf)this;
        protected ResourceBindingDesc Desc = new();

        private readonly TContinuation _continuation;
        private readonly Action<ResourceBindingDesc> _addBinding;
        private readonly List<ShaderStage> _stages = [];

        protected BindingDesc(uint registerSpace, uint binding, TContinuation continuation,
            Action<ResourceBindingDesc> addBinding)
        {
            Desc.RegisterSpace = registerSpace;
            Desc.Binding = binding;
            _continuation = continuation;
            _addBinding = addBinding;
        }

        public TSelf WithShaderStage(ShaderStage stage)
        {
            _stages.Add(stage);
            return Self;
        }

        public TContinuation Add()
        {
            _addBinding(Desc);
            return _continuation;
        }
    }

    public class Cbv<TContinuation> : BindingDesc<Cbv<TContinuation>, TContinuation>
    {
        public Cbv(uint registerSpace, uint binding, TContinuation continuation,
            Action<ResourceBindingDesc> addBinding) : base(
            registerSpace, binding, continuation, addBinding)
        {
            Desc.BindingType = ResourceBindingType.ConstantBuffer;
        }

        public Cbv<TContinuation> WithNumBytes(uint numBytes)
        {
            Desc.NumBytes = numBytes;
            return this;
        }
    }

    public class Srv<TContinuation> : BindingDesc<Srv<TContinuation>, TContinuation>
    {
        public Srv(uint registerSpace, uint binding, TContinuation continuation,
            Action<ResourceBindingDesc> addBinding) : base(registerSpace, binding, continuation, addBinding)
        {
            Desc.BindingType = ResourceBindingType.ShaderResource;
        }
        
        public Srv<TContinuation> IsBindless(bool isBindless)
        {
            Desc.IsBindless = isBindless;
            return this;
        }
    }

    public class Sampler<TContinuation> : BindingDesc<Sampler<TContinuation>, TContinuation>
    {
        public Sampler(uint registerSpace, uint binding, TContinuation continuation,
            Action<ResourceBindingDesc> addBinding) : base(
            registerSpace, binding, continuation, addBinding)
        {
            Desc.BindingType = ResourceBindingType.Sampler;
        }
    }


    public class RegisterSpaceBuilder(RootSignatureBuilder _continuation, uint registerSpace)
    {
        private List<ResourceBindingDesc> _registerSpaceBindings = [];
        
        public Cbv<RegisterSpaceBuilder> AddCbvSlot(uint binding)
        {
            return new Cbv<RegisterSpaceBuilder>(registerSpace, binding, this,
                bindingDesc => _registerSpaceBindings.Add(bindingDesc));
        }

        public Srv<RegisterSpaceBuilder> AddSrvSlot(uint binding)
        {
            return new Srv<RegisterSpaceBuilder>(registerSpace, binding, this,
                bindingDesc => _registerSpaceBindings.Add(bindingDesc));
        }
        
        public Sampler<RegisterSpaceBuilder> AddSamplerSlot(uint binding)
        {
            return new Sampler<RegisterSpaceBuilder>(registerSpace, binding, this,
                bindingDesc => _registerSpaceBindings.Add(bindingDesc));
        }

        public RootSignatureBuilder Build()
        {
            _continuation._bindings.AddRange(_registerSpaceBindings);
            return _continuation;
        }
    }

    public RegisterSpaceBuilder RegisterSpace(uint registerSpace)
    {
        return new RegisterSpaceBuilder(this, registerSpace);
    }

    public RootSignature Build()
    {
        return logicalDevice.CreateRootSignature(_rootSignatureDesc);
    }
}