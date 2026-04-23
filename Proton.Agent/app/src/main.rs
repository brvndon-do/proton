use console::style;

fn main() {
    println!(
        "Hello {}, from rust{}",
        style("proton").cyan(),
        style("!!!").red().bold()
    );
}
