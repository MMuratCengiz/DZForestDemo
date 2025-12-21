```csharp
var compilationUnit = new ShaderCompilationUnit();

compilationUnit.SetRootSignature(rootSignature);
compilationUnit.AddShaderStage(ShaderStage.Vertex, vertexShader);
compilationUnit.AddShaderStage(ShaderStage.Pixel, pixelShader);
var shaderProgram = compilationUnit.Compile();

var shader = context.NewShaderBuilder();
shader.SetProgram(shaderProgram);
shader.SetGraphicsPipelineState(pipelineState);
shader.SetRootSignature(rootSignature);
var shader = shader.Build();

```
```csharp
RenderContext context;

context.Camera;

var cmd = context.GraphicsCommandList;

cmd.SetShader(shader) // <- includes pipeline
cmd.SetUniform( "camera", camera.ViewProjectionMatrix );
for ( mesh in meshes )
{
    cmd.SetUniform( "model", mesh.ModelMatrix ); 
    cmd.DrawMesh(mesh);
}
cmd.Submit();
```