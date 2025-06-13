from flask import Flask
from flask_cors import CORS
from routes import auth, api

app = Flask(__name__)
app.secret_key = "super-secret-key"  # change in production
CORS(app, supports_credentials=True)

# Register blueprints
app.register_blueprint(auth.bp)
app.register_blueprint(api.bp)

@app.route("/")
def index():
    return "Flask backend for Unity Quiz Game is running!"

if __name__ == "__main__":
    app.run(host='0.0.0.0', port=5000, debug=True)
