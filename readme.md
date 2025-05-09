```markdown
# Educational Unity Game for Kids

This is a mobile quiz game built in Unity for educational purposes. The game displays multiple-choice questions from a JSON file and provides instant feedback.

## Features

- Portrait mobile layout
- Multiple-choice questions
- Score tracking
- JSON-based question loading
- Simple UI for children

## Project Structure

```

Assets/
├── Resources/
│   └── questions.json
├── Scenes/
│   └── QuizScene.unity
├── Scripts/
│   ├── QuizManager.cs
│   ├── Question.cs
│   └── JsonHelper.cs

````

## How to Play

1. Launch the app on a mobile device (portrait mode).
2. A question will appear with 3–4 answer options.
3. Select the answer; you’ll see feedback instantly.
4. The game continues through all questions and shows your score at the end.

## Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/SamirTaous/educational_unity_game.git
````

2. Open the project in Unity (version 2021+ recommended).

3. Ensure your build platform is set to **Android** or **iOS** in **Build Settings**.

4. Press Play or build to device.

