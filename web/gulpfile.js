
var gulp        = require('gulp'),

	coffee      = require('gulp-coffee'),
	notify      = require('gulp-notify'),
	plumber     = require('gulp-plumber'),
	slim        = require('gulp-slim'),
	smoosher    = require('gulp-smoosher'),
	stylus      = require('gulp-stylus'),
	uglify      = require('gulp-uglify')

gulp.task('slim', function () {
	return gulp.src('src/**/*.slim')
		.pipe(plumber({
			errorHandler: notify.onError('<%= error.message %>') }))
		.pipe(slim())
		.pipe(gulp.dest('src'))
})

gulp.task('stylus', function () {
	return gulp.src('src/**/*.stylus')
		.pipe(plumber({
			errorHandler: notify.onError('<%= error.message %>') }))
		.pipe(stylus({ compress: true }))
		.pipe(gulp.dest('src'))
})

gulp.task('coffee', function () {
	return gulp.src('src/**/*.coffee')
		.pipe(plumber({
			errorHandler: notify.onError('<%= error.message %>') }))
		.pipe(coffee())
		.pipe(uglify())
		.pipe(gulp.dest('src'))
})

gulp.task('html', ['slim', 'stylus', 'coffee'], function () {
	gulp.src('src/**/*.html')
		.pipe(plumber({
			errorHandler: notify.onError('<%= error.message %>') }))
		.pipe(smoosher())
		.pipe(gulp.dest('build'))
})

gulp.task('default', ['html'], function () {
	gulp.watch(['src/**/*.slim', 'src/**/*.stylus', 'src/**/*.coffee'], ['html'])
})
