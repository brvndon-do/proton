pub mod hello_world {
    tonic::include_proto!("proton.greeter_test");
}

use hello_world::HelloReply;
use hello_world::HelloRequest;
use hello_world::greeter_client::GreeterClient;

pub async fn connect_and_run() -> Result<HelloReply, Box<dyn std::error::Error>> {
    let mut client = GreeterClient::connect("http://localhost:5182").await?;

    let request = tonic::Request::new(HelloRequest {
        name: "Brandon".into(),
    });

    let response = client.say_hello(request).await?;

    Ok(response.into_inner())
}
