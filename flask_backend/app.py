from flask import Flask
from flask_cors import CORS
from routes import auth, quiz, results

app = Flask(__name__)
app.secret_key = "super-secret-key"  # change in production
CORS(app, supports_credentials=True)

# Register blueprints
app.register_blueprint(auth.bp)
app.register_blueprint(quiz.bp)
app.register_blueprint(results.bp)

@app.route("/")
def index():
    return "Flask backend for Unity Quiz Game is running!"

if __name__ == "__main__":
    app.run(debug=True)
