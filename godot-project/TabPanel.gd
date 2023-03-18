extends Panel

var projectsPanel:Panel
var installersPanel:Panel
var customInstallersPanel:Panel


func _ready():
	projectsPanel = get_node("../ProjectsPanel")
	installersPanel = get_node("../InstallersPanel")
	customInstallersPanel = get_node("../CustomInstallersPanel")
	_onProjectsButtonPressed()


func _onProjectsButtonPressed():
	projectsPanel.visible = true
	installersPanel.visible = false
	customInstallersPanel.visible = false


func _onInstallersButtonPressed():
	projectsPanel.visible = false
	installersPanel.visible = true
	customInstallersPanel.visible = false


func _onCustomInstallersButtonPressed():
	projectsPanel.visible = false
	installersPanel.visible = false
	customInstallersPanel.visible = true
