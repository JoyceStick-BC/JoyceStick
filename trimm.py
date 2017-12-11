import click
import requests
import os
import json
import shutil
import trimm_helper


@click.group()
def cli():
    pass


@cli.command()
@click.option('--bundlename', prompt='Please enter your name/package', help='Your package name in format name/package.')
@click.option('--path', help='Absolute path to install packages. Defaults to currentDir/Assets/vendor.')
@click.option('--version', help='Version number for package.')
def install(bundlename, path, version):
    if not trimm_helper.check_if_installed(bundlename, path, version):
        trimm_helper.download(bundlename, version, path)


@cli.command()
@click.option('--bundlename', prompt='Please enter your name/package', help='Your package name in format name/package.')
@click.option('--path', help='Absolute path to locate info.json. Defaults to currentDir/Assets/vendor.')
@click.option('--version', help='Version number for package.')
def update(bundlename, path, version):
    if path is None:
        path = trimm_helper.set_path()

    # if no version specified, assume latest
    if version is None:
        version = requests.get("http://trimm3d.com/latest/" + bundlename).json()["latest-version"]

    with open(path + "trimm.json", 'r') as trimm_file:
        trimm_json = json.load(trimm_file)
        trimm_assets = trimm_json["assets"]
        trimm_packages = trimm_json["packages"]

        if bundlename in trimm_assets:
            if not trimm_helper.check_if_installed(bundlename, path, version):
                trimm_helper.download(bundlename, version, path)
        elif bundlename in trimm_packages:
            if not trimm_helper.check_if_installed(bundlename, path, version):
                trimm_helper.download(bundlename, version, path)


@cli.command()
@click.option('--path', help='Absolute path to locate info.json. Defaults to currentDir/Assets/vendor.')
def pull(path):
    if path is None:
        path = trimm_helper.set_path()

    with open(path + "trimm.json", 'r') as trimm_file:
        trimm_json = json.load(trimm_file)
        trimm_assets = trimm_json["assets"]
        trimm_packages = trimm_json["packages"]

        for bundlename, version in trimm_assets.items():
            if not trimm_helper.check_if_installed(bundlename, path, None):  # , version): TODO READD version support
                trimm_helper.download(bundlename, None, path)
        for bundlename, version in trimm_packages.items():
            if not trimm_helper.check_if_installed(bundlename, path, None):  # , version): TODO READD version support
                trimm_helper.download(bundlename, None, path)


@cli.command()
@click.option('--path', help='Absolute path to locate info.json. Defaults to currentDir/Assets/vendor.')
def delete(path):
    if path is None:
        path = trimm_helper.set_path()

    with open(path + "trimm.json", 'r') as trimm_file:
        trimm_json = json.load(trimm_file)
        trimm_assets = trimm_json["assets"]

        for filename in os.listdir(path):
            new_path = os.path.join(path, filename)
            if os.path.isdir(new_path):
                for inner_filename in os.listdir(new_path):
                    bundlename = filename + "/" + inner_filename
                    if bundlename not in trimm_assets.items:
                        to_delete = os.path.join(new_path, inner_filename)
                        shutil.rmtree(to_delete)
                if not os.listdir(new_path):
                    os.rmdir(new_path)


@cli.command()
@click.option('--path', help='Absolute path to locate info.json. Defaults to currentDir/Assets/vendor.')
def make_zips(path):
    if path is None:
        path = trimm_helper.set_path()

    transfer_path = os.path.join(path, "__transfer__")

    with open(path + "trimm.json", 'r') as trimm_file:
        trimm_json = json.load(trimm_file)
        trimm_assets = trimm_json["assets"]

        for filename in os.listdir(path):
            new_path = os.path.join(path, filename)
            if filename != "__transfer__" and os.path.isdir(new_path):
                for inner_filename in os.listdir(new_path):
                    bundlename = filename + "/" + inner_filename
                    bundle_path = os.path.join(new_path, inner_filename)
                    if os.path.isdir(bundle_path) and bundlename not in trimm_assets.keys():
                        # copy bundle to a new folder
                        transfered_bundle_path = os.path.join(transfer_path, inner_filename)
                        try:
                            shutil.copytree(bundle_path, transfered_bundle_path)
                        except WindowsError:
                            print("---REMINDER---")
                            print("DELETE the transfer folder before running 'make_zips'!")
                            return

                        # delete all meta files in the transfered bundle directory
                        for item in os.listdir(transfered_bundle_path):
                            if item.endswith(".meta"):
                                os.remove(os.path.join(transfered_bundle_path, item))

                        # zip bundle
                        shutil.make_archive(base_name=transfered_bundle_path, format='zip', root_dir=transfer_path, base_dir='./' + inner_filename)

        # delete copied bundles
        if os.path.exists(transfer_path):
            for item in os.listdir(transfer_path):
                item_path = os.path.join(transfer_path, item)
                if os.path.isdir(item_path):
                    shutil.rmtree(item_path)


if __name__ == '__main__':
    cli()
