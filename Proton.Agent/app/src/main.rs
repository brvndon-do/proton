use console::style;
use dialoguer::Input;

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut name: String = String::from("");

    loop {
        if !name.trim().is_empty() {
            break;
        }

        name = Input::new()
            .with_prompt("Hello, what's your name?")
            .interact_text()?;
    }

    println!(
        "{}, {}!",
        style("Hello").yellow().italic(),
        style(name).on_green().underlined()
    );

    Ok(())
}
